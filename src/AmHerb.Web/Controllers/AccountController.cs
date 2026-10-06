using System.Text;
using System.Text.Encodings.Web;
using AmHerb.Web.Domain;
using AmHerb.Web.Models;
using AmHerb.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace AmHerb.Web.Controllers;
public class AccountController(UserManager<AppUser> users, MemberService members, IEmailSender email) : Controller
{
    [HttpGet] public IActionResult Register(string? sponsorCode) => View(new RegisterInput { SponsorCode = sponsorCode });
    [HttpPost] public async Task<IActionResult> Register(RegisterInput input)
    {
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
