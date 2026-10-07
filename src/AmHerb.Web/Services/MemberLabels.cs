using AmHerb.Web.Domain;

namespace AmHerb.Web.Services;

public static class MemberLabels
{
    public static string Order(OrderStatus status) => status switch
    {
        OrderStatus.Draft => "ฉบับร่าง", OrderStatus.PendingPayment => "รอชำระเงิน",
        OrderStatus.Paid => "ชำระแล้ว", OrderStatus.Processing => "กำลังเตรียมสินค้า",
        OrderStatus.Shipped => "จัดส่งแล้ว", OrderStatus.Delivered => "ส่งมอบแล้ว",
        OrderStatus.Completed => "สำเร็จ", OrderStatus.Cancelled => "ยกเลิกแล้ว",
        OrderStatus.ReturnRequested => "รอตรวจสอบการคืน", OrderStatus.Returned => "คืนสินค้าแล้ว",
        OrderStatus.Refunded => "คืนเงินแล้ว", _ => status.ToString()
    };
    public static string Payment(string? status) => status switch
    {
        "Pending" => "รอชำระเงิน", "Submitted" => "แจ้งโอนแล้ว · รอตรวจสอบ",
        "Confirmed" => "ยืนยันรับเงินแล้ว", "Cancelled" => "ยกเลิกแล้ว",
        "CreditApproved" => "อนุมัติเครดิต · ตรวจยอดหนี้ในบัญชีสมาชิก",
        null => "ไม่มีรายการชำระ", _ => status
    };
    public static string Member(MemberStatus status) => status switch
    {
        MemberStatus.Active => "ใช้งาน", MemberStatus.Pending => "รออนุมัติ",
        MemberStatus.Suspended => "ระงับ", MemberStatus.Closed => "ปิดบัญชี", _ => status.ToString()
    };
    public static string Token(TokenStatus status) => status switch
    {
        TokenStatus.Pending => "รอปลดล็อก", TokenStatus.Available => "พร้อมใช้",
        TokenStatus.Reserved => "จองชำระ", TokenStatus.Redeemed => "ใช้แล้ว",
        TokenStatus.Reversed => "ปรับคืน", TokenStatus.Expired => "หมดอายุ", _ => status.ToString()
    };
    public static string Ledger(LedgerKind kind) => kind switch
    {
        LedgerKind.RESERVATION => "จอง Token ชำระสินค้า", LedgerKind.RESERVATION_RELEASE => "คืน Token จากการยกเลิก",
        LedgerKind.REDEMPTION => "ยืนยันใช้ Token", LedgerKind.RELEASE => "ปลดล็อก Token",
        LedgerKind.SALE_REWARD => "รางวัลยอดขาย", LedgerKind.UPLINE_L1 => "รางวัลเครือข่ายชั้น 1",
        LedgerKind.UPLINE_L2 => "รางวัลเครือข่ายชั้น 2", LedgerKind.UPLINE_L3 => "รางวัลเครือข่ายชั้น 3",
        LedgerKind.LOYALTY => "รางวัลซื้อสินค้า", LedgerKind.PROMOTION => "รางวัลโปรโมชัน",
        LedgerKind.RETURN_REVERSAL => "ปรับยอดจากการคืนสินค้า", LedgerKind.EXPIRY => "Token หมดอายุ",
        LedgerKind.ADMIN_ADJUSTMENT => "ปรับยอดโดยเจ้าหน้าที่", _ => kind.ToString()
    };
}
