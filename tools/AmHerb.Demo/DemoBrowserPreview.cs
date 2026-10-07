using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Playwright;

public static class DemoBrowserPreview
{
    public static async Task Run(string repo)
    {
        var origin = "http://localhost:5087";
        var directory = Path.Combine(repo, "artifacts", "demo");
        var guide = await File.ReadAllTextAsync(Path.Combine(directory, "GUIDE.md"));
        var password = Regex.Match(guide, "รหัสผ่านบัญชี demo01 ถึง demo39: `([^`]+)`").Groups[1].Value;
        if (password.Length < 12) throw new InvalidOperationException("Demo guide credentials are missing.");
        var adminPassword = Environment.GetEnvironmentVariable("AMHERB_DEMO_ADMIN_PASSWORD") ?? throw new InvalidOperationException("Set AMHERB_DEMO_ADMIN_PASSWORD for local browser validation.");
        var screenshots = Path.Combine(directory, "screenshots"); Directory.CreateDirectory(screenshots);
        var checks = new List<object>(); var errors = new List<string>();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true, ExecutablePath = Environment.GetEnvironmentVariable("AMHERB_CHROME") ?? @"C:\Program Files\Google\Chrome\Application\chrome.exe" });
        async Task<IPage> Login(string email, string secret)
        {
            var context = await browser.NewContextAsync(new() { ViewportSize = new() { Width = 1440, Height = 1000 } });
            var page = await context.NewPageAsync(); page.PageError += (_, error) => errors.Add(error);
            await page.GotoAsync(origin + "/Identity/Account/Login");
            await page.Locator("#Input_Email").FillAsync(email); await page.Locator("#Input_Password").FillAsync(secret);
            await page.Locator("#login-submit").ClickAsync(); await page.WaitForURLAsync(url => !url.Contains("/Login", StringComparison.OrdinalIgnoreCase));
            return page;
        }
        async Task Visit(IPage page, string path, string screenshot)
        {
            var response = await page.GotoAsync(origin + path);
            if (response?.Status != 200 || page.Url.Contains("AccessDenied") || page.Url.Contains("/Login")) throw new InvalidOperationException($"Page check failed: {path} HTTP {response?.Status}");
            if (await page.Locator(".alert-warning").Filter(new() { HasText = "โหมดข้อมูลจำลอง" }).CountAsync() != 1) throw new InvalidOperationException("Demo notice missing.");
            await page.ScreenshotAsync(new() { Path = Path.Combine(screenshots, screenshot + ".png"), FullPage = true });
            checks.Add(new { Path = path, Status = response.Status, Screenshot = screenshot + ".png" });
        }
        var admin = await Login("admin@amherb.local", adminPassword);
        foreach (var item in new[] {
            ("/Demo", "00-demo-guide"), ("/Dashboard/Overview?from=" + DateTime.UtcNow.AddHours(7).AddDays(-89).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture), "01-overview-desktop"),
            ("/Stock", "02-stock"), ("/Admin?section=Products", "03-products"), ("/Admin?section=Prices", "04-prices"), ("/Admin?section=Orders", "05-orders"),
            ("/Admin?section=Payments", "06-payments"), ("/Admin?section=Returns", "07-returns"), ("/Admin?section=Ledger", "08-token-ledger"),
            ("/Admin?section=Tree", "09-network"), ("/Accounting", "10-accounting"), ("/Credit", "11-credit"), ("/News/Manage", "12-news"),
            ("/Messages", "13-messages"), ("/Admin?section=Imports", "14-marketplace"), ("/Transfers", "15-transfers"), ("/Admin?section=AuditLogs", "16-audit") })
            await Visit(admin, item.Item1, item.Item2);
        await admin.SetViewportSizeAsync(390, 844); await Visit(admin, "/Dashboard/Overview", "17-overview-mobile");
        if (!await admin.EvaluateAsync<bool>("document.documentElement.scrollWidth <= innerWidth + 1")) throw new InvalidOperationException("Admin mobile page overflows.");
        var member = await Login("demo06@amherb.example", password);
        foreach (var item in new[] { ("/Dashboard/Mine", "18-personal-desktop"), ("/Pos", "19-pos"), ("/Stock", "20-personal-stock"), ("/Member", "21-member-wallet"), ("/Credit", "22-personal-credit"), ("/Messages", "23-personal-messages"), ("/Catalog", "24-catalog"), ("/Cart", "25-cart") }) await Visit(member, item.Item1, item.Item2);
        await member.SetViewportSizeAsync(390, 844); await Visit(member, "/Dashboard/Mine", "26-personal-mobile");
        if (!await member.EvaluateAsync<bool>("document.documentElement.scrollWidth <= innerWidth + 1")) throw new InvalidOperationException("Member mobile page overflows.");
        foreach (var path in new[] { "/api/v1/auth/profile", "/api/v1/products", "/api/v1/cart", "/api/v1/pos", "/api/v1/orders", "/api/v1/member/tree", "/api/v1/token/balance", "/api/v1/token/ledger" })
        {
            var response = await member.Context.APIRequest.GetAsync(origin + path);
            if (response.Status != 200) throw new InvalidOperationException("API failed: " + path);
            checks.Add(new { Path = path, Status = response.Status });
        }
        await member.GotoAsync(origin + "/Demo"); if (!member.Url.Contains("AccessDenied")) throw new InvalidOperationException("Member can access demo reset management.");
        await member.GotoAsync(origin + "/Dashboard/Overview"); if (!member.Url.Contains("AccessDenied")) throw new InvalidOperationException("Member can access company dashboard.");
        foreach (var (email, path) in new[] { ("demo01@amherb.example", "/Accounting"), ("demo02@amherb.example", "/Dashboard/Overview"), ("demo03@amherb.example", "/Stock"), ("demo04@amherb.example", "/Messages") })
        {
            var staff = await Login(email, password); var response = await staff.GotoAsync(origin + path);
            if (response?.Status != 200 || staff.Url.Contains("AccessDenied")) throw new InvalidOperationException("Staff role failed: " + email);
            checks.Add(new { Role = email, Path = path, Status = response.Status });
        }
        if (errors.Count > 0) throw new InvalidOperationException(string.Join("; ", errors));
        await File.WriteAllTextAsync(Path.Combine(directory, "browser-checks.json"), JsonSerializer.Serialize(new { Checks = checks, PageErrors = errors, PermissionChecks = "Member blocked from /Demo and /Dashboard/Overview", CapturedAt = DateTime.UtcNow }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"Real AmHerb demo: {checks.Count} page/API/role checks passed; 27 screenshots captured; mobile layouts and access boundaries verified.");
    }
}
