using System.Text;
using System.Text.Encodings.Web;
using AmHerb.Web.Domain;
using AmHerb.Web.Models;
using AmHerb.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using AmHerb.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace AmHerb.Web.Controllers;
public class AccountController(UserManager<AppUser> users, MemberService members, IEmailSender email, AmHerbDbContext db) : Controller
{
    [HttpGet] public IActionResult Join() => View();
    private async Task<bool> Sponsor(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) { ViewBag.SponsorName = null; return true; }
        var sponsor = await db.Members.AsNoTracking().SingleOrDefaultAsync(x => x.ReferralCode == code && x.Status == MemberStatus.Active);
        ViewBag.SponsorName = sponsor?.Name;
        if (sponsor == null) ModelState.AddModelError(nameof(RegisterInput.SponsorCode), "รหัสแนะนำไม่ถูกต้องหรือผู้แนะนำหยุดใช้งาน กรุณาขอลิงก์ใหม่จากผู้แนะนำ");
        return sponsor != null;
    }
    [HttpGet] public async Task<IActionResult> Register(string? sponsorCode, bool withoutSponsor = false)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction(nameof(Join));
        if (withoutSponsor) sponsorCode = null;
        else if (string.IsNullOrWhiteSpace(sponsorCode) && Guid.TryParse(Request.Cookies["am.referral"], out var visitId))
        {
            var days = int.TryParse(await db.SystemSettings.Where(x => x.Key == "ReferralCookieDays").Select(x => x.Value).SingleOrDefaultAsync(), out var configuredDays) ? configuredDays : 30;
            var cutoff = DateTime.UtcNow.AddDays(-days);
            sponsorCode = await db.ReferralVisits.Where(x => x.PublicId == visitId && x.CreatedAt >= cutoff && x.Member.Status == MemberStatus.Active).Select(x => x.Member.ReferralCode).SingleOrDefaultAsync();
        }
        await Sponsor(sponsorCode);
        return View(new RegisterInput { SponsorCode = sponsorCode });
    }
    [HttpPost] public async Task<IActionResult> Register(RegisterInput input)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction(nameof(Join));
        await Sponsor(input.SponsorCode);
        if (!ModelState.IsValid) return View(input);
        var user = new AppUser { UserName = input.Email.Trim(), Email = input.Email.Trim() };
        var result = await users.CreateAsync(user, input.Password);
        if (!result.Succeeded) { foreach (var e in result.Errors) ModelState.AddModelError("", e.Description); return View(input); }
        try { await members.CreateAsync(user.Id, input.Name, input.SponsorCode); }
        catch (BusinessException e) { await users.DeleteAsync(user); ModelState.AddModelError("", e.Message); return View(input); }
        await users.AddToRoleAsync(user, "Member");
        var token = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(await users.GenerateEmailConfirmationTokenAsync(user)));
        var url = Url.Page("/Account/ConfirmEmail", null, new { area = "Identity", userId = user.Id, code = token }, Request.Scheme)!;
        try { await email.SendEmailAsync(input.Email, "ยืนยันบัญชี AM HERB", $"ยืนยันบัญชีของคุณ: <a href='{HtmlEncoder.Default.Encode(url)}'>ยืนยันอีเมล</a>"); TempData["Success"] = "สมัครสำเร็จ กรุณายืนยันอีเมลก่อนเข้าสู่ระบบ"; }
        catch (BusinessException) { TempData["Error"] = "สร้างบัญชีแล้ว แต่ยังส่งอีเมลไม่ได้ ติดต่อผู้ดูแลเพื่อตั้งค่า SMTP แล้วกดส่งอีเมลยืนยันอีกครั้ง"; }
        return Redirect("/Identity/Account/Login");
    }
}
