using AmHerb.Web.Data;
using AmHerb.Web.Domain;
using AmHerb.Web.Models;
using AmHerb.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AmHerb.Web.Controllers;
public class OrdersController(AmHerbDbContext db, Actor actor, CommerceService commerce, IAuthorizationService auth, IEnumerable<IPaymentGateway> gateways, OrderAccess access) : Controller
{
    private Task<bool> CanRead(Order o) => access.CanReadAsync(o);
    [Authorize] public async Task<IActionResult> Index() => View(await db.Orders.Where(x => x.Buyer != null && x.Buyer.UserId == actor.Id).OrderByDescending(x => x.Id).Take(200).ToListAsync());
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Detail(Guid id)
    {
        var order = await db.Orders.Include(x => x.Items).SingleOrDefaultAsync(x => x.PublicId == id);
        if (order == null || !await CanRead(order)) return NotFound();
        ViewBag.BackUrl = order.StoreId != null ? "/s/" + await db.MemberStores.Where(x => x.Id == order.StoreId).Select(x => x.Slug).SingleAsync()
            : User.Identity?.IsAuthenticated == true ? "/Member#orders" : "/Catalog";
        var payment = await db.Payments.SingleOrDefaultAsync(x => x.OrderId == order.Id);
        ViewBag.Slips = await db.PaymentSlips.Where(x => x.OrderId == order.Id).OrderByDescending(x => x.Id).Select(x => new SlipRow(x.Id, x.Reference, x.CreatedAt)).ToListAsync();
        ViewBag.CreditRefunds=payment?.Provider=="TradeCredit"
            ? await db.JournalEntries.Where(x=>x.OrderId==order.Id&&x.EventKey.StartsWith("refund:")).Select(x=>new{x.EventKey,Amount=x.Lines.Where(l=>l.Account=="1100").Sum(l=>l.Credit)}).ToDictionaryAsync(x=>x.EventKey,x=>x.Amount)
            : new Dictionary<string,decimal>();
        var intent = await gateways.First(x => x.Name == "ManualBankTransfer").CreateAsync(order.Number, order.CashPayable);
        return View(new OrderDetail(order, payment, await db.Shipments.Where(x => x.OrderId == order.Id).ToListAsync(), await db.ReturnRequests.Where(x => x.OrderItem.OrderId == order.Id).ToListAsync(), intent.Instructions, (await auth.AuthorizeAsync(User, "Orders")).Succeeded));
    }
    [HttpPost, RequestSizeLimit(6 * 1024 * 1024)] public async Task<IActionResult> SubmitPayment(Guid id, string reference, IFormFile? slip)
    {
        var order = await db.Orders.SingleOrDefaultAsync(x => x.PublicId == id);
        if (order == null || !await CanRead(order)) return NotFound();
        await commerce.SubmitPaymentAsync(order.Id, reference, slip == null ? null : await MediaService.Read(slip));
        TempData["Success"] = "แจ้งโอนแล้ว กรุณารอฝ่ายการเงินตรวจสอบ สถานะจะอัปเดตอัตโนมัติ";
        return RedirectToAction("Detail", new { id });
    }
    [HttpGet, ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Slip(long id)
    {
        var order = await db.PaymentSlips.Where(x => x.Id == id).Select(x => x.Order).SingleOrDefaultAsync();
        if (order == null || !await CanRead(order)) return NotFound();
        var slip = await db.PaymentSlips.AsNoTracking().SingleAsync(x => x.Id == id);
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return File(slip.Data, slip.ContentType);
    }
    [HttpGet] public IActionResult Track() => View();
    [HttpPost] public async Task<IActionResult> Track(string number, string phone)
    {
        var o = await db.Orders.SingleOrDefaultAsync(x => x.Number == number && x.Phone == phone);
        if (o == null) { ModelState.AddModelError("", "ไม่พบคำสั่งซื้อ"); return View(); }
        return View("Tracking", (o.Number, o.Status, await db.Shipments.Where(x => x.OrderId == o.Id).Select(x => x.Carrier + ": " + x.TrackingNumber).ToListAsync()));
    }
    [HttpPost] public async Task<IActionResult> Cancel(Guid id)
    {
        var o = await db.Orders.SingleOrDefaultAsync(x => x.PublicId == id);
        if (o == null || !await CanRead(o)) return NotFound();
        await commerce.CancelAsync(o.Id, "ยกเลิกโดยผู้ใช้"); return RedirectToAction("Detail", new { id });
    }
    [HttpPost] public async Task<IActionResult> Return(long itemId, int quantity, string reason)
    {
        var item = await db.OrderItems.Include(x => x.Order).SingleOrDefaultAsync(x => x.Id == itemId);
        if (item == null || !await CanRead(item.Order)) return NotFound();
        await commerce.RequestReturnAsync(itemId, quantity, reason); return RedirectToAction("Detail", new { id = item.Order.PublicId });
    }
}
public record SlipRow(long Id, string Reference, DateTime CreatedAt);
