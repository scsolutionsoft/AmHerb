using AmHerb.Web.Domain;
namespace AmHerb.Web.Models;
public record WorkQueue(string Title, string Value, string Url, string Hint);
public class OperationsOrders
{
    public string Origin { get; set; } = "all";
    public string Relation { get; set; } = "sales";
    public string? MemberCode { get; set; }
    public Member? Member { get; set; }
    public string? Q { get; set; }
    public OrderStatus? Status { get; set; }
    public string? Payment { get; set; }
    public bool Shipping { get; set; }
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public int Page { get; set; } = 1;
    public int Total { get; set; }
    public decimal Payable { get; set; }
    public List<Order> Orders { get; set; } = [];
    public Dictionary<long, Payment> Payments { get; set; } = [];
    public List<Shipment> Shipments { get; set; } = [];
    public Dictionary<string, Member> Cashiers { get; set; } = [];
    public static readonly Dictionary<string, string> Origins = new() { ["all"] = "ทุกแหล่งขาย", ["central"] = "ส่วนกลาง / แอดมิน", ["store"] = "ร้านออนไลน์สมาชิก", ["member-pos"] = "POS คลังสมาชิก", ["central-pos"] = "POS คลังส่วนกลาง", ["marketplace"] = "Marketplace" };
    public static readonly Dictionary<string, string> Relations = new() { ["sales"] = "ยอดขาย / ผู้แนะนำ / แคชเชียร์ของสมาชิก", ["buyer"] = "ประวัติการซื้อของสมาชิก", ["downline"] = "ยอดขายดาวน์ไลน์ทั้งหมด", ["upline"] = "ยอดขายอัพไลน์ทั้งหมด", ["reward"] = "ออเดอร์ที่เคยจัดสรร Token ให้สมาชิก" };
}
public record NetworkMember(long Id, string Code, string Name, int Depth);
public class OperationsMember
{
    public Member Member { get; set; } = null!;
    public List<NetworkMember> Upline { get; set; } = [];
    public List<NetworkMember> Downline { get; set; } = [];
    public int DownlineTotal { get; set; }
    public int Page { get; set; }
    public bool Finance { get; set; }
    public bool Tokens { get; set; }
    public CreditAccount? Credit { get; set; }
    public decimal Debt { get; set; }
    public decimal Overdue { get; set; }
    public decimal Pending { get; set; }
    public decimal Available { get; set; }
    public decimal Reserved { get; set; }
    public List<TokenLedger> Ledger { get; set; } = [];
}
public record ClearingRow(long Id, long MemberId, string Code, string Name, decimal Balance, decimal Overdue, int PendingReceipts);
public class ClearingPage
{
    public string? Q { get; set; }
    public bool OverdueOnly { get; set; }
    public int Page { get; set; }
    public int Total { get; set; }
    public decimal Balance { get; set; }
    public decimal Overdue { get; set; }
    public int PendingReceipts { get; set; }
    public List<ClearingRow> Rows { get; set; } = [];
    public List<PosSession> PosDifferences { get; set; } = [];
}
