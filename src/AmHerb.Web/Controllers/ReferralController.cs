using AmHerb.Web.Data;
using AmHerb.Web.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AmHerb.Web.Controllers;
public class ReferralController(AmHerbDbContext db) : Controller
{
    [HttpGet("r/{code}")]
    public async Task<IActionResult> Visit(string code, string? source, string? campaign, string? landing)
    {
        var member = await db.Members.SingleOrDefaultAsync(x => x.ReferralCode == code && x.Status == MemberStatus.Active);
        if (member == null) return NotFound();
        var destination = Url.IsLocalUrl(landing) ? landing! : "/Catalog";
        var visit = new ReferralVisit { MemberId = member.Id, Source = (source ?? "")[..Math.Min(100, source?.Length ?? 0)], Campaign = (campaign ?? "")[..Math.Min(100, campaign?.Length ?? 0)], LandingPage = destination[..Math.Min(500, destination.Length)] };
        db.ReferralVisits.Add(visit); await db.SaveChangesAsync();
        var days = int.Parse((await db.SystemSettings.SingleAsync(x => x.Key == "ReferralCookieDays")).Value);
        Response.Cookies.Append("am.referral", visit.PublicId.ToString(), new CookieOptions { HttpOnly = true, Secure = Request.IsHttps, SameSite = SameSiteMode.Lax, MaxAge = TimeSpan.FromDays(days) });
        return LocalRedirect(destination);
    }
}
