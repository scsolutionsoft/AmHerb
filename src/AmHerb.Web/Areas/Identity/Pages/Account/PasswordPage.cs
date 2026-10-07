using System.ComponentModel.DataAnnotations;
using System.Text;
using AmHerb.Web.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
namespace AmHerb.Web.Areas.Identity.Pages.Account;
public class PasswordPage(UserManager<AppUser> users, SignInManager<AppUser> signIn) : PageModel
{
    public bool IsReset => Request.Path.Value?.EndsWith("/ResetPassword", StringComparison.OrdinalIgnoreCase) == true;
    public bool IsSet => Request.Path.Value?.EndsWith("/SetPassword", StringComparison.OrdinalIgnoreCase) == true;
    public string Heading => IsReset ? "ตั้งรหัสผ่านใหม่" : IsSet ? "ตั้งรหัสผ่านสำหรับบัญชี" : "เปลี่ยนรหัสผ่าน";
    [BindProperty] public PasswordInput Input { get; set; } = new();
    public class PasswordInput
    {
        [EmailAddress] public string? Email { get; set; }
        public string? Code { get; set; }
        [DataType(DataType.Password)] public string? OldPassword { get; set; }
        [Required, StringLength(10, MinimumLength = 5, ErrorMessage = "รหัสผ่านต้องมี 5–10 ตัวอักษร"), DataType(DataType.Password)] public string Password { get; set; } = "";
        [Required, Compare(nameof(Password), ErrorMessage = "รหัสผ่านทั้งสองช่องต้องตรงกัน"), DataType(DataType.Password)] public string ConfirmPassword { get; set; } = "";
    }
    public async Task<IActionResult> OnGetAsync(string? code = null, string? email = null)
    {
        if (IsReset)
        {
            if (string.IsNullOrWhiteSpace(code)) return RedirectToPage("/Account/ForgotPassword");
            try { Input.Code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code)); }
            catch (FormatException) { return BadRequest("ลิงก์ตั้งรหัสผ่านไม่ถูกต้อง"); }
            Input.Email = email;
        }
        else
        {
            var user = await users.GetUserAsync(User); if (user == null) return Challenge();
            var hasPassword = await users.HasPasswordAsync(user);
            if (IsSet && hasPassword) return RedirectToPage("/Account/Manage/ChangePassword");
            if (!IsSet && !hasPassword) return RedirectToPage("/Account/Manage/SetPassword");
        }
        return Page();
    }
    public async Task<IActionResult> OnPostAsync()
    {
        AppUser? user = null;
        if (!IsReset) { user = await users.GetUserAsync(User); if (user == null) return Challenge(); }
        if (IsReset && (string.IsNullOrWhiteSpace(Input.Email) || string.IsNullOrWhiteSpace(Input.Code))) ModelState.AddModelError("", "กรอกอีเมลและเปิดลิงก์ตั้งรหัสผ่านจากอีเมลของคุณ");
        if (!IsReset && !IsSet && string.IsNullOrEmpty(Input.OldPassword)) ModelState.AddModelError("Input.OldPassword", "กรอกรหัสผ่านปัจจุบัน");
        if (!ModelState.IsValid) return Page();
        IdentityResult result;
        if (IsReset)
        {
            user = await users.FindByEmailAsync(Input.Email!.Trim());
            if (user == null) return RedirectToPage("/Account/ResetPasswordConfirmation");
            result = await users.ResetPasswordAsync(user, Input.Code!, Input.Password);
        }
        else result = IsSet ? await users.AddPasswordAsync(user!, Input.Password) : await users.ChangePasswordAsync(user!, Input.OldPassword!, Input.Password);
        if (!result.Succeeded) { foreach (var error in result.Errors) ModelState.AddModelError("", error.Description); return Page(); }
        if (IsReset) return RedirectToPage("/Account/ResetPasswordConfirmation");
        await signIn.RefreshSignInAsync(user!);
        TempData["Success"] = "บันทึกรหัสผ่านใหม่แล้ว";
        return LocalRedirect("/Member#profile");
    }
}
