using AmHerb.Web.Domain;
using Microsoft.AspNetCore.Identity;
namespace AmHerb.Web.Services;
public class SimplePasswordValidator : IPasswordValidator<AppUser>
{
    public Task<IdentityResult> ValidateAsync(UserManager<AppUser> manager, AppUser user, string? password) =>
        Task.FromResult(password is { Length: >= 5 and <= 10 } && !string.IsNullOrWhiteSpace(password)
            ? IdentityResult.Success
            : IdentityResult.Failed(new IdentityError { Code = "PasswordLength", Description = "ตั้งรหัสผ่าน 5–10 ตัวอักษร ไม่ต้องมีตัวใหญ่ ตัวเลข หรือสัญลักษณ์" }));
}
