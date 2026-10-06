using AmHerb.Web.Data;
using AmHerb.Web.Domain;
using AmHerb.Web.Models;
using AmHerb.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AmHerb.Web.Controllers;
public class CartController(AmHerbDbContext db, CartService carts, CommerceService commerce, MemberService members, RewardService rewards, Actor actor, OrderAccess access) : Controller
{
    public async Task<IActionResult> Index()
    {
        var model = new CartModel { Lines = await carts.LinesAsync(await carts.GetAsync()) };
        var shipping = await db.ShippingRules.Where(x => x.Active).OrderBy(x => x.Id).FirstAsync();
        model.ShippingFee = model.Total >= shipping.FreeAbove ? 0 : shipping.Fee;
        if (User.Identity?.IsAuthenticated == true)
        { var m = await members.RequireAsync(actor.Id); model.Wallet = await rewards.BalanceAsync(m.Id); model.Checkout.CustomerName = m.Name; model.Checkout.Phone = m.Phone; model.Checkout.Address = m.Address; model.Checkout.Email = User.Identity.Name; }
        if(User.Identity?.IsAuthenticated==true)
        {
            var credit=await db.CreditAccounts.SingleOrDefaultAsync(x=>x.Member.UserId==actor.Id&&x.Enabled);
            if(credit!=null){model.CreditAvailable=credit.Limit-await db.CreditInvoices.Where(x=>x.CreditAccountId==credit.Id).SumAsync(x=>x.Amount-x.Paid-x.Adjusted);model.CreditDays=credit.TermDays;}
        }
        return View(model);
    }
    [HttpPost] public async Task<IActionResult> Add(long skuId, int quantity = 1, string tier = "Retail") { await carts.SetAsync(skuId, quantity, true, tier); return RedirectToAction("Index"); }
    [HttpPost] public async Task<IActionResult> Update(long skuId, int quantity, string tier = "Retail") { await carts.SetAsync(skuId, quantity, false, tier); return RedirectToAction("Index"); }
    [HttpPost] public async Task<IActionResult> Checkout([Bind(Prefix = "Checkout")] CheckoutInput input)
    {
        if (!ModelState.IsValid) { TempData["Error"] = "กรุณาตรวจข้อมูลผู้รับและจำนวน Token"; return RedirectToAction("Index"); }
        var cart = await carts.GetAsync();
        var buyer = User.Identity?.IsAuthenticated == true ? await members.RequireAsync(actor.Id) : null;
        ReferralVisit? visit = null;
        if (Guid.TryParse(Request.Cookies["am.referral"], out var visitId))
            visit = await db.ReferralVisits.Include(x => x.Member).SingleOrDefaultAsync(x => x.PublicId == visitId && x.Member.Status == MemberStatus.Active);
        var days = int.Parse((await db.SystemSettings.SingleAsync(x => x.Key == "ReferralCookieDays")).Value);
        if (visit != null && visit.CreatedAt < DateTime.UtcNow.AddDays(-days)) visit = null;
        var order = await commerce.CheckoutAsync(input, cart.Items.Select(x => new SaleLine(x.SkuId, x.Quantity, x.Tier)).ToArray(), buyer?.Id, visit?.MemberId, campaign: visit?.Campaign ?? "");
        if (visit != null) visit.ConvertedOrderId = order.Id;
        db.CartItems.RemoveRange(cart.Items); await db.SaveChangesAsync();
        access.GrantGuest(order);
        return RedirectToAction("Detail", "Orders", new { id = order.PublicId });
    }
}
