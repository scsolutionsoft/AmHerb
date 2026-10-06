using AmHerb.Web.Data;
using AmHerb.Web.Domain;
using Microsoft.EntityFrameworkCore;

namespace AmHerb.Web.Services;

public interface IRecurringJobs { Task RunAsync(CancellationToken ct); }
public class RecurringJobs(AmHerbDbContext db, RewardService rewards, CommerceService commerce, INotificationService notifications,
    IEnumerable<IMarketplaceProvider> marketplaces, TimeProvider clock) : IRecurringJobs
{
    public async Task RunAsync(CancellationToken ct)
    {
        await rewards.RunDueAsync();
        var now = clock.GetUtcNow().UtcDateTime;
        var currentPolicy = await db.TokenPolicies.Where(x => x.EffectiveFrom <= now).OrderByDescending(x => x.EffectiveFrom).FirstAsync(ct);
        var effectiveSettings = new Dictionary<string, string> {
            ["RewardPendingDays"] = currentPolicy.PendingDays.ToString(), ["MaxUplineRewardDepth"] = currentPolicy.MaxDepth.ToString(),
            ["TokenRedemptionReferenceTHB"] = currentPolicy.RedemptionReferenceThb.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["TokenExpiryMonths"] = currentPolicy.ExpiryMonths.ToString(), ["MaxTokenRedemptionPercent"] = currentPolicy.MaxRedemptionPercent.ToString(System.Globalization.CultureInfo.InvariantCulture)
        };
        foreach (var settingRow in await db.SystemSettings.ToListAsync(ct)) if (effectiveSettings.TryGetValue(settingRow.Key, out var value)) settingRow.Value = value;
        var setting = await db.SystemSettings.SingleAsync(x => x.Key == "AbandonedCheckoutHours", ct);
        var cutoff = now.AddHours(-int.Parse(setting.Value, System.Globalization.CultureInfo.InvariantCulture));
        foreach (var id in await db.Orders.Where(x => x.Status == OrderStatus.PendingPayment && x.CreatedAt < cutoff).Select(x => x.Id).ToListAsync(ct))
            await commerce.CancelAsync(id, "หมดเวลารอชำระเงิน");
        var carts = await db.Carts.Include(x => x.Items).Where(x => x.UpdatedAt < now.AddDays(-30)).Take(500).ToListAsync(ct);
        db.CartItems.RemoveRange(carts.SelectMany(x => x.Items)); db.Carts.RemoveRange(carts);
        foreach (var b in await db.InventoryBatches.Include(x => x.Sku).Include(x => x.Warehouse).Where(x => x.QtyAvailable > 0 && x.ExpDate <= now.AddDays(90)).ToListAsync(ct))
        {
            var days = (b.ExpDate - now).TotalDays; var band = days <= 0 ? 0 : days <= 30 ? 30 : days <= 60 ? 60 : 90;
            await notifications.AddAsync(b.Warehouse.OwnerMemberId, $"expiry:{b.Id}:{band}", $"{b.Warehouse.Name}: {b.Sku.Code} ล็อต {b.LotNo} หมดอายุ {Bangkok.Format(b.ExpDate)} (ช่วง {band} วัน)");
        }
        var stockGroups = await db.InventoryBatches.Where(x => x.Warehouse.Active && x.Sku.Active)
            .GroupBy(x => new { x.WarehouseId, x.Warehouse.Name, x.Warehouse.OwnerMemberId, x.SkuId, x.Sku.Code, x.Sku.LowStockThreshold })
            .Select(g => new { g.Key, Available = g.Sum(x => x.ExpDate > now ? x.QtyAvailable : 0) }).ToListAsync(ct);
        foreach (var stock in stockGroups.Where(x => x.Available <= x.Key.LowStockThreshold))
            await notifications.AddAsync(stock.Key.OwnerMemberId, $"low:{stock.Key.WarehouseId}:{stock.Key.SkuId}:{now:yyyyMMdd}", $"{stock.Key.Name}: {stock.Key.Code} พร้อมขาย {stock.Available}");
        var day = now.Date;
        var snapshot = await db.ReportSnapshots.SingleOrDefaultAsync(x => x.Day == day, ct);
        if (snapshot == null) { snapshot = new ReportSnapshot { Day = day }; db.ReportSnapshots.Add(snapshot); }
        var payments = await db.Payments.Where(x => x.Status == "Confirmed" && x.ConfirmedAt >= day && x.ConfirmedAt < day.AddDays(1)).ToListAsync(ct);
        snapshot.Orders = payments.Count; snapshot.Sales = payments.Sum(x => x.Amount);
        snapshot.TokenLiability = (await db.TokenLedger.SumAsync(x => (decimal?)(x.AvailableDelta + x.PendingDelta + x.ReservedDelta), ct)) ?? 0;
        await db.SaveChangesAsync(ct);
        foreach (var marketplace in marketplaces.Where(x => x.Configured)) await marketplace.SyncAsync(ct);
    }
}
public class CommerceWorker(IServiceScopeFactory scopes, ILogger<CommerceWorker> logger, IConfiguration config) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!config.GetValue("Jobs:Enabled", true)) return;
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(60));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try { await using var scope = scopes.CreateAsyncScope(); await scope.ServiceProvider.GetRequiredService<IRecurringJobs>().RunAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception e) { logger.LogError(e, "Commerce jobs failed; transaction will retry on next interval."); }
        }
    }
}
