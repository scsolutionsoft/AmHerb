using AmHerb.Web.Data;
using AmHerb.Web.Domain;
using AmHerb.Web.Models;
using AmHerb.Web.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AmHerb.Web.Controllers;
public record CartUpdate(long SkuId, int Quantity, string Tier = "Retail");
public record QuoteInput(decimal EligibleMerchandise, SalesChannel Channel);
public record LoginInput(string Email, string Password);
public record RedemptionQuote(decimal MaximumTokens, decimal MerchandiseValue);
public record PosOpenInput(decimal OpeningCash);
public record PosCloseInput(long SessionId, decimal CountedCash, string Note);
public record PosConsentInput(long RegisterId, decimal Tokens);
[ApiController, Route("api/v1")]
public class ApiController(AmHerbDbContext db, StoreQueries queries, CartService carts, MemberService members,
    RewardService rewards, Actor actor, SignInManager<AppUser> signIn, IAntiforgery antiforgery, CommerceService commerce, PosService pos, OrderAccess access, RedemptionConsentService consent) : ControllerBase
{
    [HttpGet("auth/csrf")] public IActionResult Csrf() => Ok(new { token = antiforgery.GetAndStoreTokens(HttpContext).RequestToken });
    [HttpPost("auth/login")] public async Task<IActionResult> Login(LoginInput input)
    {
        var result = await signIn.PasswordSignInAsync(input.Email, input.Password, false, true);
        if (result.RequiresTwoFactor) return Conflict(new { message = "Complete MFA through /Identity/Account/LoginWith2fa" });
        return result.Succeeded ? Ok(new { authenticated = true }) : Unauthorized(new { message = "Sign-in failed" });
    }
    [HttpPost("auth/logout"), Authorize] public async Task<IActionResult> Logout() { await signIn.SignOutAsync(); return NoContent(); }
    [HttpGet("auth/profile"), Authorize] public async Task<IActionResult> Profile()
    { var m = await members.RequireAsync(actor.Id); return Ok(new { m.Code, m.Name, m.Phone, m.Address, status = m.Status.ToString(), type = m.Type.ToString() }); }
    [HttpGet("products")] public async Task<IActionResult> Products(string? q) => Ok(await queries.CatalogAsync(q));
    [HttpGet("products/{id:long}/prices")] public async Task<IActionResult> Prices(long id)
    {
        var now = DateTime.UtcNow; var authenticated = User.Identity?.IsAuthenticated == true;
        return Ok(await db.ProductPrices.Where(x => x.SkuId == id && (authenticated || x.Tier == "Retail") && x.EffectiveFrom <= now && (x.EffectiveTo == null || x.EffectiveTo > now)).Select(x => new { x.Id, x.Tier, x.PackQuantity, x.Amount, x.Channel, x.EffectiveFrom, x.EffectiveTo }).ToListAsync());
    }
    [HttpGet("cart")] public async Task<IActionResult> Cart() => Ok(await carts.LinesAsync(await carts.GetAsync()));
    [HttpPost("cart")] public async Task<IActionResult> Cart(CartUpdate input) { await carts.SetAsync(input.SkuId, input.Quantity, false, input.Tier); return Ok(await carts.LinesAsync(await carts.GetAsync())); }
    [HttpPost("orders")]
    public async Task<IActionResult> Checkout(CheckoutInput input)
    {
        var cart = await carts.GetAsync(); var buyer = User.Identity?.IsAuthenticated == true ? await members.RequireAsync(actor.Id) : null;
        ReferralVisit? visit = null;
        if (Guid.TryParse(Request.Cookies["am.referral"], out var visitId)) visit = await db.ReferralVisits.SingleOrDefaultAsync(x => x.PublicId == visitId && x.Member.Status == MemberStatus.Active);
        var days = int.Parse((await db.SystemSettings.SingleAsync(x => x.Key == "ReferralCookieDays")).Value);
        if (visit != null && visit.CreatedAt < DateTime.UtcNow.AddDays(-days)) visit = null;
        var order = await commerce.CheckoutAsync(input, cart.Items.Select(x => new SaleLine(x.SkuId, x.Quantity, x.Tier)).ToArray(), buyer?.Id, visit?.MemberId, campaign: visit?.Campaign ?? "");
        if (visit != null) visit.ConvertedOrderId = order.Id;
        db.CartItems.RemoveRange(cart.Items); await db.SaveChangesAsync(); access.GrantGuest(order);
        return Ok(new { order.PublicId, order.Number, order.Status, order.CashPayable, order.TokenRedemption });
    }
    [HttpGet("orders/{id:guid}")]
    public async Task<IActionResult> Order(Guid id)
    {
        var order = await db.Orders.Include(x => x.Items).SingleOrDefaultAsync(x => x.PublicId == id);
        if (order == null || !await access.CanReadAsync(order)) return NotFound();
        return Ok(new { order.PublicId, order.Number, order.Status, order.GrandTotal, order.CashPayable, order.TokenRedemption, items = order.Items.Select(x => new { x.Name, x.Quantity, x.UnitPrice, x.Discount, x.ReturnedQuantity }) });
    }
    [HttpGet("pos"), Authorize]
    public async Task<IActionResult> Pos()
    {
        var register = await pos.RegisterAsync(actor.Id);
        var sessions = await db.PosSessions.Where(x => x.PosRegisterId == register.Id).OrderByDescending(x => x.Id).Take(30).Select(x => new { x.Id, x.OpenedAt, x.ClosedAt, x.OpeningCash, x.ExpectedCash, x.CountedCash, x.Difference }).ToListAsync();
        return Ok(new { register.Id, register.Name, register.WarehouseId, sessions });
    }
    [HttpPost("pos/open"), Authorize] public async Task<IActionResult> Open(PosOpenInput input) { await pos.OpenAsync(actor.Id, input.OpeningCash); return NoContent(); }
    [HttpPost("pos/close"), Authorize] public async Task<IActionResult> Close(PosCloseInput input) { await pos.CloseAsync(actor.Id, input.SessionId, input.CountedCash, input.Note); return NoContent(); }
    [HttpPost("pos/sales"), Authorize]
    public async Task<IActionResult> PosSale(PosSaleInput input)
    {
        if (input.SkuIds.Length != input.Quantities.Length) return BadRequest();
        var seller = await members.RequireAsync(actor.Id);
        var buyer = string.IsNullOrWhiteSpace(input.BuyerCode) ? null : await db.Members.SingleOrDefaultAsync(x => x.Code == input.BuyerCode && x.Status == MemberStatus.Active) ?? throw new BusinessException("ไม่พบสมาชิกผู้ซื้อ");
        var order = await commerce.CheckoutAsync(input, input.SkuIds.Zip(input.Quantities, (id, qty) => new SaleLine(id, qty)).Where(x => x.Quantity > 0).ToArray(), buyer?.Id, seller.Id, SalesChannel.POS, actor.Id, input.SessionId, input.Tendered, input.PaymentMethod);
        return Ok(new { order.PublicId, order.Number, order.CashPayable, order.Status });
    }
    [HttpPost("redemption/pos-consent"), Authorize] public async Task<IActionResult> Consent(PosConsentInput input)
    { var member = await members.RequireAsync(actor.Id); return Ok(new { code = await consent.IssueAsync(member.Id, input.RegisterId, input.Tokens), expiresInSeconds = 300 }); }
    [HttpGet("orders"), Authorize] public async Task<IActionResult> Orders()
    { var m = await members.RequireAsync(actor.Id); return Ok(await db.Orders.Where(x => x.BuyerMemberId == m.Id).OrderByDescending(x => x.Id).Take(200).Select(x => new { x.PublicId, x.Number, x.Status, x.GrandTotal, x.TokenRedemption, x.CashPayable, x.CreatedAt }).ToListAsync()); }
    [HttpGet("member/tree"), Authorize] public async Task<IActionResult> Tree()
    { var m = await members.RequireAsync(actor.Id); return Ok(await db.MemberClosures.Where(x => x.AncestorMemberId == m.Id).Select(x => new TreeNode(x.DescendantMemberId, x.Descendant.SponsorMemberId, x.Descendant.Code, x.Descendant.Name, x.Depth, x.Descendant.Status)).ToListAsync()); }
    [HttpGet("token/balance"), Authorize] public async Task<IActionResult> Balance() => Ok(await rewards.BalanceAsync((await members.RequireAsync(actor.Id)).Id));
    [HttpGet("token/ledger"), Authorize] public async Task<IActionResult> Ledger(int page = 1)
    { var m = await members.RequireAsync(actor.Id); return Ok(await db.TokenLedger.Where(x => x.MemberId == m.Id).OrderByDescending(x => x.Id).Skip((Math.Clamp(page, 1, 100000) - 1) * 100).Take(100).Select(x => new { x.Id, x.Kind, x.Status, x.PendingDelta, x.AvailableDelta, x.ReservedDelta, x.PostedAt, x.SourceOrderId }).ToListAsync()); }
    [HttpPost("redemption/quote"), Authorize] public async Task<IActionResult> Quote(QuoteInput input)
    { if (input.EligibleMerchandise < 0 || !Enum.IsDefined(input.Channel)) return BadRequest(); var m = await members.RequireAsync(actor.Id); return Ok(new RedemptionQuote(await rewards.QuoteAsync(m.Id, input.EligibleMerchandise, input.Channel), input.EligibleMerchandise)); }
    [HttpGet("referral/attribution")] public async Task<IActionResult> Attribution()
    {
        if (!Guid.TryParse(Request.Cookies["am.referral"], out var id)) return Ok(new { attributed = false });
        var v = await db.ReferralVisits.Where(x => x.PublicId == id && x.Member.Status == MemberStatus.Active).Select(x => new { x.Member.ReferralCode, x.Campaign, x.Source, x.CreatedAt }).SingleOrDefaultAsync();
        return Ok(v);
    }
    [HttpPost("marketplace/{channel}/webhook"), IgnoreAntiforgeryToken]
    public IActionResult Webhook(string channel) => StatusCode(501, new { message = "Provider credentials and signature verification must be configured before enabling this endpoint." });
}
