using System.Globalization;
using System.Text;
using AmHerb.Web.Data;
using AmHerb.Web.Domain;
using AmHerb.Web.Services;
using CsvHelper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AmHerb.Tests;
[Collection("SQL")]
public class MarketplaceTests(SqlFixture fixture)
{
    [SqlFact] public async Task CSV_maps_SKUs_combines_lines_updates_fulfillment_and_is_idempotent()
    {
        await using var h = new Harness(fixture); await h.InitializeAsync();
        await h.Inventory.ReceiveAsync(h.SkuId, 1, "CSV", h.Clock.Now.AddDays(-20), h.Clock.Now.AddYears(1), 10, 100, "CSV test stock");
        var code = (await h.Db.Skus.SingleAsync(x => x.Id == h.SkuId)).Code;
        var services = new ServiceCollection(); services.AddDbContext<AmHerbDbContext>(o => o.UseSqlServer(fixture.Connection));
        services.AddSingleton<TimeProvider>(h.Clock); services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.AddScoped<Actor>(); services.AddScoped<AuditService>(); services.AddScoped<PricingService>(); services.AddScoped<RewardService>(); services.AddScoped<InventoryService>(); services.AddScoped<CommerceService>(); services.AddScoped<INotificationService, NotificationService>();
        await using var provider = services.BuildServiceProvider(); var importer = new CsvMarketplaceImportProvider(provider.GetRequiredService<IServiceScopeFactory>());
        var external = "CSV-" + Guid.NewGuid().ToString("N");
        var row = new MarketplaceRow { Channel = "Shopee", ExternalOrderId = external, Sku = code, Quantity = 1, CustomerName = "CSV customer", Phone = "0900000000", Address = "CSV address", FulfillmentStatus = "Delivered", TransactionRef = external, PaidAmount = 1980, Carrier = "Test carrier", TrackingNumber = external, ChannelFee = 40, AffiliateFee = 20 };
        using var text = new StringWriter(CultureInfo.InvariantCulture); using (var writer = new CsvWriter(text, CultureInfo.InvariantCulture, true)) { writer.WriteRecords(new[] { row, row }); }
        async Task<IReadOnlyList<string>> Import() { using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text.ToString())); return await importer.ImportAsync(stream, "test-import"); }
        Assert.Contains(": OK ", (await Import()).Single()); Assert.Contains(": OK ", (await Import()).Single());
        await using var verify = fixture.Db(); var order = await verify.Orders.Include(x => x.Items).SingleAsync(x => x.Channel == SalesChannel.Shopee && x.ExternalOrderId == external);
        Assert.Equal(OrderStatus.Delivered, order.Status); Assert.Equal(2, order.Items.Single().Quantity); Assert.Equal(40, order.ChannelFee); Assert.False(order.VerifiedRetailSale);
        Assert.Equal(1, await verify.Payments.CountAsync(x => x.OrderId == order.Id)); Assert.Equal(8, await verify.InventoryBatches.Where(x => x.SkuId == h.SkuId && x.WarehouseId == 1).SumAsync(x => x.QtyAvailable));
    }
}
