using AmHerb.Web.Data;
using AmHerb.Web.Domain;
using AmHerb.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace AmHerb.Web.Services;

public class SalesDashboardService(AmHerbDbContext db, TimeProvider clock)
{
    public static IQueryable<Order> ScopeOrders(IQueryable<Order> source, string? userId) => userId == null ? source :
        source.Where(x => x.CashierUserId == userId || (x.Seller != null && x.Seller.UserId == userId && x.Seller.Status == MemberStatus.Active));
    public static IQueryable<Order> Sales(IQueryable<Order> source) => source.Where(x =>
        x.Status == OrderStatus.Paid || x.Status == OrderStatus.Processing || x.Status == OrderStatus.Shipped ||
        x.Status == OrderStatus.Delivered || x.Status == OrderStatus.Completed || x.Status == OrderStatus.ReturnRequested ||
        x.Status == OrderStatus.Returned || x.Status == OrderStatus.Refunded);

    public async Task<SalesDashboard> BuildAsync(string? userId, DateTime? from, DateTime? to, SalesChannel? channel)
    {
        if (channel != null && !Enum.IsDefined(channel.Value)) throw new BusinessException("ช่องทางการขายไม่ถูกต้อง");
        var now = clock.GetUtcNow().UtcDateTime;
        var period = DashboardPeriod.Create(from, to, now);
        var previousStart = period.StartUtc.AddDays(-period.Days);
        var scope = ScopeOrders(db.Orders.AsNoTracking(), userId);
        if (channel != null) scope = scope.Where(x => x.Channel == channel);
        var sales = Sales(scope);
        var facts = await sales.Where(x => x.CreatedAt >= previousStart && x.CreatedAt < period.EndUtc)
            .Select(x => new SalesFact(x.Id, x.Number, x.CreatedAt, x.Channel, x.Campaign, x.Status, x.BuyerMemberId,
                x.Items.Sum(i => i.Quantity == 0 ? 0 : (i.UnitPrice * i.Quantity - i.Discount) * (i.Quantity - i.ReturnedQuantity) / i.Quantity),
                userId == null ? x.Items.SelectMany(i => i.Allocations).Sum(a => a.Quantity * a.InventoryBatch.Cost)
                    - db.InventoryTransactions.Where(t => t.OrderId == x.Id && t.Kind == InventoryKind.Return).Sum(t => t.Quantity * t.InventoryBatch.Cost) : 0,
                x.ChannelFee + x.AffiliateFee, x.Discount, x.Items.Sum(i => i.Quantity - i.ReturnedQuantity)))
            .ToListAsync();
        var current = facts.Where(x => x.CreatedAt >= period.StartUtc).ToList();
        var previous = facts.Where(x => x.CreatedAt < period.StartUtc).ToList();
        var daily = current.GroupBy(x => x.CreatedAt.AddHours(7).Date).ToDictionary(g => g.Key, g => g.Sum(x => x.Net));
        var previousDaily = previous.GroupBy(x => x.CreatedAt.AddHours(7).Date).ToDictionary(g => g.Key, g => g.Sum(x => x.Net));
        var periodSales = sales.Where(x => x.CreatedAt >= period.StartUtc && x.CreatedAt < period.EndUtc);
        var productRows = await periodSales.SelectMany(x => x.Items)
            .GroupBy(x => new { x.SkuId, x.Sku.Code, x.Sku.Product.Name })
            .Select(g => new { g.Key.SkuId, g.Key.Code, g.Key.Name, Units = g.Sum(i => i.Quantity - i.ReturnedQuantity),
                Net = g.Sum(i => i.Quantity == 0 ? 0 : (i.UnitPrice * i.Quantity - i.Discount) * (i.Quantity - i.ReturnedQuantity) / i.Quantity) })
            .OrderByDescending(x => x.Net).Take(10).ToListAsync();
        var products = productRows.Select(x => new DashboardProduct(x.SkuId, x.Code, x.Name, x.Units, x.Net)).ToList();
        var warehouses = db.Warehouses.AsNoTracking().Where(x => x.Active);
        if (userId != null) warehouses = warehouses.Where(x => x.OwnerMember != null && x.OwnerMember.UserId == userId && x.OwnerMember.Status == MemberStatus.Active);
        var warehouseIds = warehouses.Select(x => x.Id);
        var stockRows = await db.InventoryBatches.AsNoTracking().Where(x => warehouseIds.Contains(x.WarehouseId))
            .GroupBy(x => new { x.WarehouseId, Warehouse = x.Warehouse.Name, x.SkuId, x.Sku.Code, x.Sku.Product.Name, x.Sku.LowStockThreshold })
            .Select(g => new { g.Key.SkuId, g.Key.Code, g.Key.Name, g.Key.Warehouse,
                Available = g.Sum(x => x.ExpDate > now ? x.QtyAvailable : 0), Reserved = g.Sum(x => x.QtyReserved),
                Expiring = g.Sum(x => x.ExpDate > now && x.ExpDate <= now.AddDays(90) ? x.QtyAvailable : 0),
                Expired = g.Sum(x => x.ExpDate <= now ? x.QtyAvailable : 0), Threshold = g.Key.LowStockThreshold })
            .OrderBy(x => x.Available).ThenBy(x => x.Code).ToListAsync();
        var stock = stockRows.Select(x => new DashboardStock(x.SkuId, x.Code, x.Name, x.Warehouse, x.Available, x.Reserved, x.Expiring, x.Expired, x.Threshold)).ToList();
        var visits = db.ReferralVisits.AsNoTracking().Where(x => x.CreatedAt >= period.StartUtc && x.CreatedAt < period.EndUtc);
        if (userId != null) visits = visits.Where(x => x.Member.UserId == userId && x.Member.Status == MemberStatus.Active);
        // Visit cohort is independent of the channel filter; conversions match selected paid sales.
        var model = new SalesDashboard
        {
            Overview = userId == null, Period = period, Channel = channel,
            Current = DashboardSummary.From(current), Previous = DashboardSummary.From(previous),
            Days = Enumerable.Range(0, period.Days).Select(i =>
            {
                var day = period.From.AddDays(i);
                return new SalesDay(day, daily.GetValueOrDefault(day), previousDaily.GetValueOrDefault(day.AddDays(-period.Days)));
            }).ToList(),
            Products = products, Stock = stock,
            Channels = current.GroupBy(x => x.Channel).Select(g => new SalesBreakdown(SalesDashboard.ChannelLabel(g.Key), g.Count(), g.Sum(x => x.Units), g.Sum(x => x.Net), g.Sum(x => x.Fees))).OrderByDescending(x => x.Net).ToList(),
            Campaigns = current.GroupBy(x => string.IsNullOrWhiteSpace(x.Campaign) ? "ไม่มีแคมเปญ" : x.Campaign).Select(g => new SalesBreakdown(g.Key, g.Count(), g.Sum(x => x.Units), g.Sum(x => x.Net), g.Sum(x => x.Fees))).OrderByDescending(x => x.Net).Take(10).ToList(),
            Recent = current.OrderByDescending(x => x.CreatedAt).Take(8).ToList(),
            Visits = await visits.CountAsync(),
            ConvertedVisits = await visits.CountAsync(x => x.ConvertedOrderId != null && periodSales.Any(o => o.Id == x.ConvertedOrderId)),
            PendingOrders = await scope.CountAsync(x => x.Status == OrderStatus.PendingPayment && x.CreatedAt >= period.StartUtc && x.CreatedAt < period.EndUtc),
            NewMembers = userId == null ? await db.Members.CountAsync(x => x.CreatedAt >= period.StartUtc && x.CreatedAt < period.EndUtc) : 0
        };
        if (model.Current.Orders == 0) model.Insights.Add("ยังไม่มียอดขายที่ชำระหรืออนุมัติเครดิตแล้วในช่วงนี้ ลองขยายช่วงวันที่ หรือเปิด POS เพื่อเริ่มขาย");
        if (products.Count > 0) model.Insights.Add($"{products[0].Name} ทำยอดขายสูงสุด {products[0].Net:N2} บาท รวม {products[0].Units:N0} ชิ้น พิจารณาเตรียมสต็อกก่อนจัดโปรโมชัน");
        if (model.Channels.Count > 0 && model.Current.Net > 0) model.Insights.Add($"{model.Channels[0].Label} เป็นช่องทางหลัก คิดเป็น {model.Channels[0].Net / model.Current.Net * 100:N1}% ของยอดขายสุทธิ");
        var low = stock.Count(x => x.Available <= x.Threshold);
        if (low > 0) model.Insights.Add($"มี {low:N0} รายการสินค้าในคลังที่ถึงจุดเตือนสต็อกต่ำ ตรวจและเติมสินค้าก่อนเริ่มแคมเปญ");
        var expiring = stock.Sum(x => x.Expiring);
        if (expiring > 0) model.Insights.Add($"มีสินค้า {expiring:N0} ชิ้นใกล้หมดอายุใน 90 วัน ควรวางแผนขายตามล็อตที่หมดอายุก่อน");
        if (model.PendingOrders > 0) model.Insights.Add($"มี {model.PendingOrders:N0} คำสั่งซื้อรอชำระในช่วงที่เลือก ควรติดตามการชำระเงิน");
        return model;
    }
}
