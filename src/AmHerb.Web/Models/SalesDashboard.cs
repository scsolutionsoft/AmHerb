using AmHerb.Web.Domain;

namespace AmHerb.Web.Models;

public record DashboardPeriod(DateTime From, DateTime To)
{
    public int Days => (To - From).Days + 1;
    public DateTime StartUtc => DateTime.SpecifyKind(From.AddHours(-7), DateTimeKind.Utc);
    public DateTime EndUtc => DateTime.SpecifyKind(To.AddDays(1).AddHours(-7), DateTimeKind.Utc);
    public static DashboardPeriod Create(DateTime? from, DateTime? to, DateTime utcNow)
    {
        var today = utcNow.AddHours(7).Date;
        var end = to?.Date ?? today;
        var start = from?.Date ?? end.AddDays(-29);
        if (start > end || (end - start).Days >= 366 || start.Year < 2000 || end > today)
            throw new Services.BusinessException("กรุณาเลือกช่วงวันที่ไม่เกิน 366 วัน โดยวันเริ่มต้นไม่เกินวันสิ้นสุด และไม่เลือกวันที่ในอนาคต");
        return new(start, end);
    }
}
public record SalesFact(long Id, string Number, DateTime CreatedAt, SalesChannel Channel, string Campaign,
    OrderStatus Status, long? BuyerMemberId, decimal Net, decimal Cost, decimal Fees, decimal Discount, int Units);
public record DashboardSummary(decimal Net, int Orders, int Units, decimal Cost, decimal Fees, decimal Discount, int RepeatBuyers)
{
    public decimal Average => Orders == 0 ? 0 : Net / Orders;
    public decimal Contribution => Net - Cost - Fees;
    public static DashboardSummary From(IEnumerable<SalesFact> source)
    {
        var rows = source.ToList();
        return new(rows.Sum(x => x.Net), rows.Count, rows.Sum(x => x.Units), rows.Sum(x => x.Cost),
            rows.Sum(x => x.Fees), rows.Sum(x => x.Discount),
            rows.Where(x => x.BuyerMemberId != null).GroupBy(x => x.BuyerMemberId).Count(g => g.Count() > 1));
    }
    public static decimal? Growth(decimal current, decimal previous) => previous == 0 ? null : (current - previous) / Math.Abs(previous) * 100;
}
public record SalesDay(DateTime Day, decimal Net, decimal Previous);
public record SalesBreakdown(string Label, int Orders, int Units, decimal Net, decimal Fees);
public record DashboardProduct(long SkuId, string Code, string Name, int Units, decimal Net);
public record DashboardStock(long SkuId, string Code, string Name, string Warehouse, int Available, int Reserved, int Expiring, int Expired, int Threshold);
public class SalesDashboard
{
    public bool Overview { get; init; }
    public DashboardPeriod Period { get; init; } = null!;
    public SalesChannel? Channel { get; init; }
    public DashboardSummary Current { get; init; } = null!;
    public DashboardSummary Previous { get; init; } = null!;
    public List<SalesDay> Days { get; init; } = [];
    public List<SalesBreakdown> Channels { get; init; } = [];
    public List<SalesBreakdown> Campaigns { get; init; } = [];
    public List<DashboardProduct> Products { get; init; } = [];
    public List<DashboardStock> Stock { get; init; } = [];
    public List<SalesFact> Recent { get; init; } = [];
    public List<string> Insights { get; set; } = [];
    public int Visits { get; init; }
    public int ConvertedVisits { get; init; }
    public decimal ConversionRate => Visits == 0 ? 0 : 100m * ConvertedVisits / Visits;
    public int PendingOrders { get; init; }
    public int NewMembers { get; init; }
    public static string ChannelLabel(SalesChannel channel) => channel switch
    {
        SalesChannel.Store => "ร้านค้าออนไลน์", SalesChannel.POS => "POS", SalesChannel.Manual => "ขายตรง / Manual",
        SalesChannel.TikTokShop => "TikTok Shop", _ => channel.ToString()
    };
    public static string StatusLabel(OrderStatus status) => status switch
    {
        OrderStatus.Paid => "ชำระแล้ว", OrderStatus.Processing => "กำลังจัดสินค้า", OrderStatus.Shipped => "จัดส่งแล้ว",
        OrderStatus.Delivered => "ส่งมอบแล้ว", OrderStatus.Completed => "สำเร็จ", OrderStatus.ReturnRequested => "ขอคืนสินค้า",
        OrderStatus.Returned => "คืนสินค้าแล้ว", OrderStatus.Refunded => "คืนเงินแล้ว", _ => status.ToString()
    };
}
