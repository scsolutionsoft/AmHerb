using AmHerb.Web.Data;
using AmHerb.Web.Domain;
using AmHerb.Web.Models;
using AmHerb.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;

namespace AmHerb.Web.Controllers;
[Authorize]
public class MemberController(AmHerbDbContext db, MemberService members, RewardService rewards, Actor actor, UserManager<AppUser> users, RedemptionConsentService consent) : Controller
{
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Index()
    {
        if (!await db.Members.AnyAsync(x => x.UserId == actor.Id))
        { var user = await users.GetUserAsync(User); await members.CreateAsync(actor.Id, user?.Email ?? "Member", null); await users.AddToRoleAsync(user!, "Member"); }
        var m = await members.RequireAsync(actor.Id);
        ViewData["PaymentStatuses"] = await db.Payments.Where(x => x.Order.BuyerMemberId == m.Id || x.Order.CashierUserId == actor.Id)
            .OrderByDescending(x => x.OrderId).Take(100).ToDictionaryAsync(x => x.OrderId, x => x.Status);
        var descendants = await db.MemberClosures.Where(x => x.AncestorMemberId == m.Id && x.Depth > 0).Select(x => x.DescendantMemberId).ToArrayAsync();
        var personal = await db.OrderItems.Where(x => x.Order.SellerMemberId == m.Id && x.Order.VerifiedRetailSale).SumAsync(x => (decimal?)((x.UnitPrice * x.Quantity - x.Discount) * (x.Quantity - x.ReturnedQuantity) / x.Quantity)) ?? 0;
        return View(new MemberDashboard { Member = m, Wallet = await rewards.BalanceAsync(m.Id),
            DirectDownlines = await db.Members.CountAsync(x => x.SponsorMemberId == m.Id), ActiveDownlines = await db.Members.CountAsync(x => descendants.Contains(x.Id) && x.Status == MemberStatus.Active),
            PersonalSales = personal,
            TeamSales = await db.OrderItems.Where(x => x.Order.SellerMemberId != null && descendants.Contains(x.Order.SellerMemberId.Value) && x.Order.VerifiedRetailSale).SumAsync(x => (decimal?)((x.UnitPrice * x.Quantity - x.Discount) * (x.Quantity - x.ReturnedQuantity) / x.Quantity)) ?? 0,
            Orders = await db.Orders.Where(x => x.BuyerMemberId == m.Id || x.SellerMemberId == m.Id || x.CashierUserId == actor.Id).OrderByDescending(x => x.Id).Take(100).ToListAsync(),
            Ledger = await db.TokenLedger.Where(x => x.MemberId == m.Id).OrderByDescending(x => x.Id).Take(200).ToListAsync(),
            Tree = await db.MemberClosures.Where(x => x.AncestorMemberId == m.Id).OrderBy(x => x.Depth).Select(x => new TreeNode(x.DescendantMemberId, x.Descendant.SponsorMemberId, x.Descendant.Code, x.Descendant.Name, x.Depth, x.Descendant.Status)).ToListAsync(),
            Upline = await db.MemberClosures.Where(x => x.DescendantMemberId == m.Id && x.Depth > 0).OrderByDescending(x => x.Depth).Select(x => new TreeNode(x.AncestorMemberId, x.Ancestor.SponsorMemberId, x.Ancestor.Code, x.Ancestor.Name, x.Depth, x.Ancestor.Status)).ToListAsync(),
            Notifications = await db.Notifications.Where(x => x.UserId == actor.Id).OrderByDescending(x => x.Id).Take(30).ToListAsync(),
            ReferralUrl = Url.Action("Visit", "Referral", new { code = m.ReferralCode }, Request.Scheme)! });
    }
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Downline(long id)
    {
        var member = await members.RequireAsync(actor.Id);
        var node = await db.MemberClosures.Where(x => x.AncestorMemberId == member.Id && x.DescendantMemberId == id && x.Depth > 0)
            .Select(x => new TreeNode(x.DescendantMemberId, x.Descendant.SponsorMemberId, x.Descendant.Code, x.Descendant.Name, x.Depth, x.Descendant.Status)).SingleOrDefaultAsync();
        if (node == null) return NotFound();
        var children = await db.Members.Where(x => x.SponsorMemberId == id)
            .Select(x => new TreeNode(x.Id, x.SponsorMemberId, x.Code, x.Name, node.Depth + 1, x.Status)).ToListAsync();
        return View(new DownlineDetail(node, children));
    }
    // Referral sellers receive only a status summary, never buyer contact/payment data.
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Sale(Guid id)
    {
        var member = await members.RequireAsync(actor.Id);
        var order = await db.Orders.Where(x => x.PublicId == id && x.SellerMemberId == member.Id)
            .Select(x => new MemberSale(x.Number, x.Status, x.CreatedAt, x.Merchandise - x.Discount)).SingleOrDefaultAsync();
        return order == null ? NotFound() : View(order);
    }
    public async Task<IActionResult> Qr()
    {
        var m = await members.RequireAsync(actor.Id); var url = Url.Action("Visit", "Referral", new { code = m.ReferralCode }, Request.Scheme)!;
        using var generator = new QRCodeGenerator(); using var data = generator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
        using var code = new PngByteQRCode(data); return File(code.GetGraphic(8), "image/png");
    }
    [HttpPost] public async Task<IActionResult> Profile(string name, string phone, string address)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 160 || (phone?.Length ?? 0) > 40 || (address?.Length ?? 0) > 1000) throw new BusinessException("ข้อมูลโปรไฟล์ไม่ถูกต้อง");
        var m = await members.RequireAsync(actor.Id); m.Name = name; m.Phone = phone ?? ""; m.Address = address ?? "";
        await db.SaveChangesAsync(); TempData["Success"] = "บันทึกโปรไฟล์แล้ว"; return RedirectToAction("Index");
    }
    [HttpPost] public async Task<IActionResult> Read(long id)
    { var n = await db.Notifications.SingleOrDefaultAsync(x => x.Id == id && x.UserId == actor.Id); if (n != null) { n.Read = true; await db.SaveChangesAsync(); } return RedirectToAction("Index"); }
    [HttpPost] public async Task<IActionResult> AuthorizeRedemption(long registerId, decimal tokens)
    { var m = await members.RequireAsync(actor.Id); TempData["PosConsent"] = await consent.IssueAsync(m.Id, registerId, tokens); return RedirectToAction("Index"); }
}
