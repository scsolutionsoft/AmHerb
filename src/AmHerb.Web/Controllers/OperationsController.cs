using AmHerb.Web.Data;
using AmHerb.Web.Domain;
using AmHerb.Web.Models;
using AmHerb.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace AmHerb.Web.Controllers;
[Authorize(Policy = "BackOffice")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class OperationsController(AmHerbDbContext db, IAuthorizationService authorization, CommerceService commerce, IConfiguration configuration) : Controller
{
    private async Task<bool> Can(string policy) => (await authorization.AuthorizeAsync(User, policy)).Succeeded;
    public async Task<IActionResult> Index()
    {
        var rows = new List<WorkQueue>();
        if (await Can("Orders"))
        {
            rows.Add(new("คำสั่งซื้อทั้งหมด", (await db.Orders.CountAsync()).ToString("N0"), "/Operations/Orders", "แยกส่วนกลาง ร้านสมาชิก POS และ Marketplace"));
            rows.Add(new("รอจัดส่ง", (await db.Orders.CountAsync(x => x.Channel != SalesChannel.POS && (x.Status == OrderStatus.Paid || x.Status == OrderStatus.Processing))).ToString("N0"), "/Operations/Orders?shipping=true", "ชำระแล้ว / อนุมัติเครดิต → เตรียม → ส่ง"));
        }
        if (await Can("Payments"))
        {
            rows.Add(new("สลิปรอตรวจ", (await db.Payments.CountAsync(x => x.Status == "Submitted" && x.Order.Status == OrderStatus.PendingPayment)).ToString("N0"), "/Operations/Orders?payment=Submitted", "ตรวจหลักฐานก่อนยืนยันรับเงิน"));
            rows.Add(new("ลูกหนี้สมาชิกคงค้าง", (await db.CreditInvoices.SumAsync(x => (decimal?)(x.Amount - x.Paid - x.Adjusted)) ?? 0).ToString("N2"), "/Operations/Clearing", "ยอดหนี้เครดิตหลังหักยอดรับชำระและปรับปรุง"));
        }
        if (await Can("Inventory"))
        {
            rows.Add(new("คลังสินค้า", (await db.Warehouses.CountAsync(x => x.Active)).ToString("N0"), "/Stock", "แยกคลังส่วนกลาง สมาชิก และดีลเลอร์"));
            rows.Add(new("ใบเบิกรอดำเนินการ", (await db.StockTransfers.CountAsync(x => x.Status == TransferStatus.Requested || x.Status == TransferStatus.Dispatched)).ToString("N0"), "/Transfers", "จ่ายจากคลัง → ระหว่างทาง → รับเข้าคลัง"));
        }
        if (await Can("Members")) rows.Add(new("สมาชิก", (await db.Members.CountAsync()).ToString("N0"), "/Admin?section=Members", "เปิดรายคนเพื่อตรวจอัพไลน์ ดาวน์ไลน์ และประวัติ"));
        if (await Can("Tokens")) rows.Add(new("สมาชิกต้องตรวจ Token", (await db.TokenLedger.GroupBy(x => x.MemberId).Where(g => g.Sum(x => x.AvailableDelta) < 0).CountAsync()).ToString("N0"), "/Operations/Tokens?negative=true", "แยกยอดพร้อมใช้ รอปลดล็อก และจอง"));
        return View(rows);
    }
    [Authorize(Policy = "Orders")]
    public async Task<IActionResult> Orders(string origin = "all", string relation = "sales", string? memberCode = null, string? q = null, OrderStatus? status = null, string? payment = null, bool shipping = false, DateTime? from = null, DateTime? to = null, int page = 1)
    {
        var today = DateTime.UtcNow.AddHours(7).Date;
        return View(await new OperationsQueries(db).Orders(new OperationsOrders { Origin = origin, Relation = relation, MemberCode = memberCode, Q = q, Status = status, Payment = payment, Shipping = shipping, From = (from ?? new DateTime(2000, 1, 1)).Date, To = (to ?? today).Date, Page = page }));
    }
    [HttpPost, Authorize(Policy = "Orders")]
    public async Task<IActionResult> Prepare(long id)
    {
        if (!await db.Orders.AnyAsync(x => x.Id == id && x.Channel != SalesChannel.POS)) return NotFound();
        await commerce.PrepareAsync(id); TempData["Success"] = "เริ่มเตรียมสินค้าแล้ว"; return RedirectToAction(nameof(Orders), new { shipping = true });
    }
    [HttpPost, Authorize(Policy = "Orders")]
    public async Task<IActionResult> Ship(long id, string? carrier, string? tracking, bool delivered, string reason)
    {
        if (!await db.Orders.AnyAsync(x => x.Id == id && x.Channel != SalesChannel.POS)) return NotFound();
        if ((carrier?.Length ?? 0) > 120 || (tracking?.Length ?? 0) > 120 || string.IsNullOrWhiteSpace(reason)) throw new BusinessException("ตรวจชื่อผู้ขนส่ง เลขพัสดุ และเหตุผลดำเนินการ");
        await commerce.FulfillAsync(id, carrier ?? "", tracking ?? "", delivered, reason);
        TempData["Success"] = "อัปเดตการจัดส่งแล้ว"; return RedirectToAction(nameof(Orders), new { shipping = true });
    }
    [Authorize(Policy = "Orders")]
    public async Task<IActionResult> Label(long id)
    {
        var order = await db.Orders.AsNoTracking().Include(x => x.Store).ThenInclude(x => x!.Member).SingleOrDefaultAsync(x => x.Id == id && x.Channel != SalesChannel.POS);
        if (order == null) return NotFound(); ViewBag.LabelBackUrl = "/Operations/Orders?shipping=true";
        return View("~/Views/MyStore/ShippingLabel.cshtml", new ShippingLabelPage(order, order.Store?.Name ?? "AM HERB", order.Store?.Phone ?? configuration["Shipping:SenderPhone"] ?? "", order.Store?.Member.Address ?? configuration["Shipping:SenderAddress"] ?? ""));
    }
    public async Task<IActionResult> Member(long id, int page = 1)
    {
        var finance = await Can("Payments"); var tokens = await Can("Tokens");
        if (!finance && !tokens && !await Can("Members")) return Forbid();
        var member = await db.Members.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id); if (member == null) return NotFound();
        page = Math.Clamp(page, 1, 100000);
        var children = db.MemberClosures.Where(x => x.AncestorMemberId == id && x.Depth > 0);
        var model = new OperationsMember { Member = member, Finance = finance, Tokens = tokens, Page = page,
            Upline = await db.MemberClosures.Where(x => x.DescendantMemberId == id && x.Depth > 0).OrderBy(x => x.Depth).Select(x => new NetworkMember(x.AncestorMemberId, x.Ancestor.Code, x.Ancestor.Name, x.Depth)).ToListAsync(),
            DownlineTotal = await children.CountAsync(), Downline = await children.OrderBy(x => x.Depth).ThenBy(x => x.DescendantMemberId).Skip((page - 1) * 50).Take(50).Select(x => new NetworkMember(x.DescendantMemberId, x.Descendant.Code, x.Descendant.Name, x.Depth)).ToListAsync() };
        if (tokens)
        {
            var ledger = db.TokenLedger.Where(x => x.MemberId == id);
            model.Pending = await ledger.SumAsync(x => (decimal?)x.PendingDelta) ?? 0; model.Available = await ledger.SumAsync(x => (decimal?)x.AvailableDelta) ?? 0; model.Reserved = await ledger.SumAsync(x => (decimal?)x.ReservedDelta) ?? 0;
            model.Ledger = await ledger.Include(x => x.SourceOrder).OrderByDescending(x => x.Id).Take(30).ToListAsync();
        }
        if (finance)
        {
            model.Credit = await db.CreditAccounts.AsNoTracking().SingleOrDefaultAsync(x => x.MemberId == id);
            var invoices = db.CreditInvoices.Where(x => x.CreditAccount.MemberId == id);
            model.Debt = await invoices.SumAsync(x => (decimal?)(x.Amount - x.Paid - x.Adjusted)) ?? 0;
            model.Overdue = await invoices.Where(x => x.DueAt < DateTime.UtcNow).SumAsync(x => (decimal?)(x.Amount - x.Paid - x.Adjusted)) ?? 0;
        }
        return View(model);
    }
    [Authorize(Policy = "Tokens")]
    public async Task<IActionResult> Tokens(string? q, bool negative = false, int page = 1)
    {
        page = Math.Clamp(page, 1, 100000); var members = db.Members.AsNoTracking().Where(x => q == null || x.Code.Contains(q) || x.Name.Contains(q));
        if (negative) members = members.Where(x => (db.TokenLedger.Where(l => l.MemberId == x.Id).Sum(l => (decimal?)l.AvailableDelta) ?? 0) < 0);
        ViewBag.Q = q; ViewBag.Negative = negative; ViewBag.Page = page; ViewBag.Total = await members.CountAsync();
        var rows = await members.OrderBy(x => x.Code).Skip((page - 1) * 50).Take(50).Select(x => new OperationsMember { Member = x,
            Pending = db.TokenLedger.Where(l => l.MemberId == x.Id).Sum(l => (decimal?)l.PendingDelta) ?? 0,
            Available = db.TokenLedger.Where(l => l.MemberId == x.Id).Sum(l => (decimal?)l.AvailableDelta) ?? 0,
            Reserved = db.TokenLedger.Where(l => l.MemberId == x.Id).Sum(l => (decimal?)l.ReservedDelta) ?? 0 }).ToListAsync();
        return View(rows);
    }
    [Authorize(Policy = "Payments")]
    public async Task<IActionResult> Clearing(string? q, bool overdue = false, int page = 1)
    {
        var now = DateTime.UtcNow; page = Math.Clamp(page, 1, 100000);
        var query = db.CreditAccounts.AsNoTracking().Where(x => q == null || x.Member.Code.Contains(q) || x.Member.Name.Contains(q))
            .Select(x => new { x.Id, x.MemberId, x.Member.Code, x.Member.Name, Balance =
                db.CreditInvoices.Where(i => i.CreditAccountId == x.Id).Sum(i => (decimal?)(i.Amount - i.Paid - i.Adjusted)) ?? 0,
                Overdue = db.CreditInvoices.Where(i => i.CreditAccountId == x.Id && i.DueAt < now).Sum(i => (decimal?)(i.Amount - i.Paid - i.Adjusted)) ?? 0,
                PendingReceipts = db.CreditReceipts.Count(r => r.CreditAccountId == x.Id && r.Status == CreditReceiptStatus.Pending) });
        if (overdue) query = query.Where(x => x.Overdue > 0);
        var matching = query.Select(x => x.Id);
        var invoices = db.CreditInvoices.Where(x => matching.Contains(x.CreditAccountId));
        return View(new ClearingPage { Q = q, OverdueOnly = overdue, Page = page, Total = await query.CountAsync(), Balance = await invoices.SumAsync(x => (decimal?)(x.Amount - x.Paid - x.Adjusted)) ?? 0,
            Overdue = await invoices.Where(x => x.DueAt < now).SumAsync(x => (decimal?)(x.Amount - x.Paid - x.Adjusted)) ?? 0, PendingReceipts = await db.CreditReceipts.CountAsync(x => matching.Contains(x.CreditAccountId) && x.Status == CreditReceiptStatus.Pending),
            Rows = await query.OrderByDescending(x => x.Overdue).ThenBy(x => x.Code).Skip((page - 1) * 50).Take(50).Select(x => new ClearingRow(x.Id, x.MemberId, x.Code, x.Name, x.Balance, x.Overdue, x.PendingReceipts)).ToListAsync(),
            PosDifferences = await db.PosSessions.AsNoTracking().Include(x => x.PosRegister).Where(x => x.ClosedAt != null && x.Difference != null && x.Difference != 0 && (q == null || db.Members.Any(m => m.UserId == x.PosRegister.UserId && (m.Code.Contains(q) || m.Name.Contains(q))))).OrderByDescending(x => x.Id).Take(30).ToListAsync() });
    }
}
