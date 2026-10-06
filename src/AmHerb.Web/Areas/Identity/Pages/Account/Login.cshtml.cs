using System.ComponentModel.DataAnnotations;
using AmHerb.Web.Domain;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
namespace AmHerb.Web.Areas.Identity.Pages.Account;
[AllowAnonymous]
public class LoginModel(SignInManager<AppUser> signIn):PageModel
{
    [BindProperty]public LoginInput Input {get;set;}=new();
    public string ReturnUrl {get;set;}="/Member";
    public IList<AuthenticationScheme> ExternalLogins {get;set;}=[];
    public class LoginInput { [Required,EmailAddress] public string Email{get;set;}=""; [Required,DataType(DataType.Password)]public string Password{get;set;}=""; public bool RememberMe{get;set;} }
    public async Task OnGetAsync(string? returnUrl=null){ReturnUrl=Url.IsLocalUrl(returnUrl)?returnUrl!:"/Member";await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);ExternalLogins=(await signIn.GetExternalAuthenticationSchemesAsync()).ToList();}
    public async Task<IActionResult> OnPostAsync(string? returnUrl=null)
    {
        ReturnUrl=Url.IsLocalUrl(returnUrl)?returnUrl!:"/Member";ExternalLogins=(await signIn.GetExternalAuthenticationSchemesAsync()).ToList();if(!ModelState.IsValid)return Page();
        var result=await signIn.PasswordSignInAsync(Input.Email.Trim(),Input.Password,Input.RememberMe,lockoutOnFailure:true);
        if(result.Succeeded)return LocalRedirect(ReturnUrl);
        if(result.RequiresTwoFactor)return RedirectToPage("./LoginWith2fa",new{ReturnUrl,RememberMe=Input.RememberMe});
        if(result.IsLockedOut)return RedirectToPage("./Lockout");
        ModelState.AddModelError("",result.IsNotAllowed?"กรุณายืนยันอีเมลก่อนเข้าสู่ระบบ":"อีเมลหรือรหัสผ่านไม่ถูกต้อง");return Page();
    }
}
