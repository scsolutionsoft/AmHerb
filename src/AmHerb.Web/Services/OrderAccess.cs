using AmHerb.Web.Data;
using AmHerb.Web.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace AmHerb.Web.Services;
public class OrderAccess(AmHerbDbContext db, Actor actor, IAuthorizationService auth, IDataProtectionProvider protection, IHttpContextAccessor accessor)
{
    private IDataProtector Protector => protection.CreateProtector("AMHERB.GuestOrderAccess.v1");
    public void GrantGuest(Order o)
    {
        var context = accessor.HttpContext!;
        context.Response.Cookies.Append("am.order." + o.PublicId, Protector.Protect(o.PublicId.ToString()), new CookieOptions { HttpOnly = true, Secure = context.Request.IsHttps, SameSite = SameSiteMode.Lax, MaxAge = TimeSpan.FromDays(30) });
    }
    public async Task<bool> CanReadAsync(Order o)
    {
        var context = accessor.HttpContext!;
        if (o.StoreId != null && await db.MemberStores.AnyAsync(x => x.Id == o.StoreId && x.Member.UserId == actor.Id && x.Member.Status == MemberStatus.Active)) return true;
        if (o.CashierUserId == actor.Id || (o.BuyerMemberId != null && await db.Members.AnyAsync(x => x.Id == o.BuyerMemberId && x.UserId == actor.Id)) || (await auth.AuthorizeAsync(context.User, "Orders")).Succeeded || (await auth.AuthorizeAsync(context.User, "Refunds")).Succeeded) return true;
        var cookie = context.Request.Cookies["am.order." + o.PublicId];
        if (cookie == null) return false;
        try { return Protector.Unprotect(cookie) == o.PublicId.ToString(); }
        catch (System.Security.Cryptography.CryptographicException) { return false; }
    }
}
