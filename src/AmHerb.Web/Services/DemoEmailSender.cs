using System.Text.Json;
using AmHerb.Web.Data;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
namespace AmHerb.Web.Services;
public class DemoEmailSender(IWebHostEnvironment environment, AmHerbDbContext db, IConfiguration configuration) : IEmailSender
{
    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        if (!environment.IsDevelopment()) throw new BusinessException("อีเมลจำลองใช้ได้เฉพาะ Development");
        if (!await db.SystemSettings.AnyAsync(x => x.Key == "Demo.State"))
        { await new SmtpEmailSender(configuration).SendEmailAsync(email, subject, htmlMessage); return; }
        var directory = Path.GetFullPath(Path.Combine(environment.ContentRootPath, "../../artifacts/demo/outbox"));
        Directory.CreateDirectory(directory);
        var message = new { To = email, Subject = subject, Html = htmlMessage, CreatedAt = DateTime.UtcNow, Simulated = true };
        await File.WriteAllTextAsync(Path.Combine(directory, Guid.NewGuid().ToString("N") + ".json"), JsonSerializer.Serialize(message));
    }
}
