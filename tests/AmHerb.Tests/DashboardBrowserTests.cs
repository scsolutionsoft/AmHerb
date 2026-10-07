using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using AmHerb.Web.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;

namespace AmHerb.Tests;

[Collection("SQL")]
public class DashboardBrowserTests(SqlFixture fixture)
{
    [BrowserFact] public async Task Dashboards_render_on_desktop_mobile_and_enforce_global_report_access()
    {
        await using var h = new Harness(fixture); await h.InitializeAsync();
        var admin = await h.MemberAsync(); var owner = await h.MemberAsync(); var outsider = await h.MemberAsync();
        var password = "T!" + Guid.NewGuid().ToString("N") + "a9";
        foreach (var member in new[] { admin, owner, outsider })
        {
            var user = await h.Db.Users.SingleAsync(x => x.Id == member.UserId);
            user.SecurityStamp = Guid.NewGuid().ToString();
            user.PasswordHash = new PasswordHasher<AppUser>().HashPassword(user, password);
        }
        var role = await h.Db.Roles.SingleOrDefaultAsync(x => x.Name == "SuperAdmin");
        if (role == null) { role = new IdentityRole("SuperAdmin") { NormalizedName = "SUPERADMIN" }; h.Db.Roles.Add(role); }
        await h.Db.SaveChangesAsync();
        h.Db.UserRoles.Add(new IdentityUserRole<string> { UserId = admin.UserId, RoleId = role.Id }); await h.Db.SaveChangesAsync();
        await h.SellAsync(owner);
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "AmHerb.sln"))) root = root.Parent;
        Assert.NotNull(root);
        var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port; listener.Stop();
        var origin = "http://127.0.0.1:" + port;
        var processInfo = new ProcessStartInfo("dotnet") { WorkingDirectory = Path.Combine(root!.FullName, "src", "AmHerb.Web"), UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
        processInfo.ArgumentList.Add(typeof(Program).Assembly.Location); processInfo.ArgumentList.Add("--urls"); processInfo.ArgumentList.Add(origin);
        processInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Testing";
        processInfo.Environment["ConnectionStrings__DefaultConnection"] = fixture.Connection;
        processInfo.Environment["Jobs__Enabled"] = "false";
        processInfo.Environment["Logging__LogLevel__Default"] = "Warning";
        using var process = Process.Start(processInfo)!;
        var stdout = process.StandardOutput.ReadToEndAsync(); var stderr = process.StandardError.ReadToEndAsync();
        try
        {
            using var client = new HttpClient();
            var ready = false;
            for (var attempt = 0; attempt < 60; attempt++)
            {
                try { if ((await client.GetAsync(origin)).IsSuccessStatusCode) { ready = true; break; } } catch (HttpRequestException) { }
                await Task.Delay(500);
            }
            Assert.True(ready, "Browser test web server did not start.");
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true, ExecutablePath = Environment.GetEnvironmentVariable("AMHERB_CHROME") ?? @"C:\Program Files\Google\Chrome\Application\chrome.exe" });
            var screenshots = Path.Combine(root.FullName, "artifacts", "screenshots"); Directory.CreateDirectory(screenshots);
            async Task<IPage> Login(Member member)
            {
                var context = await browser.NewContextAsync(new() { ViewportSize = new() { Width = 1440, Height = 1000 } });
                var page = await context.NewPageAsync();
                await page.GotoAsync(origin + "/Identity/Account/Login");
                await page.Locator("#Input_Email").FillAsync((await h.Db.Users.SingleAsync(x => x.Id == member.UserId)).Email!);
                await page.Locator("#Input_Password").FillAsync(password);
                await page.Locator("#login-submit").ClickAsync(); await page.WaitForURLAsync(url => !url.Contains("/Login", StringComparison.OrdinalIgnoreCase));
                return page;
            }
            var ownerPage = await Login(owner); var errors = new List<string>(); ownerPage.PageError += (_, error) => errors.Add(error);
            var ownResponse = await ownerPage.GotoAsync(origin + "/Dashboard/Mine"); Assert.Equal(200, ownResponse!.Status);
            Assert.Equal(1, await ownerPage.Locator("#sales-chart svg").CountAsync());
            Assert.Contains("990", await ownerPage.Locator(".dash-kpis").InnerTextAsync());
            Assert.DoesNotContain("กำไรหลังต้นทุน", await ownerPage.Locator(".sales-dashboard").InnerTextAsync());
            await ownerPage.Locator("#chart-day").FillAsync("0"); Assert.Contains("ยอดขาย", await ownerPage.Locator("#chart-detail").InnerTextAsync());
            await ownerPage.Locator("#compare-sales").UncheckAsync();
            Assert.DoesNotContain("วันเทียบเคียง", await ownerPage.Locator("#chart-detail").InnerTextAsync());
            await ownerPage.ScreenshotAsync(new() { Path = Path.Combine(screenshots, "dashboard-mine-desktop.png"), FullPage = true });
            await ownerPage.SetViewportSizeAsync(390, 844);
            Assert.True(await ownerPage.EvaluateAsync<bool>("document.documentElement.scrollWidth <= window.innerWidth + 1"));
            await ownerPage.ScreenshotAsync(new() { Path = Path.Combine(screenshots, "dashboard-mine-mobile.png"), FullPage = true });
            var ownCsvStatus = await ownerPage.EvaluateAsync<int>("async () => (await fetch('/Dashboard/ExportMine')).status"); Assert.Equal(200, ownCsvStatus);
            await ownerPage.GotoAsync(origin + "/Dashboard/Overview"); Assert.Contains("AccessDenied", ownerPage.Url);
            await ownerPage.GotoAsync(origin + "/Dashboard/ExportOverview"); Assert.Contains("AccessDenied", ownerPage.Url);
            var outsiderPage = await Login(outsider); await outsiderPage.GotoAsync(origin + "/Dashboard/Mine?userId=" + owner.UserId);
            Assert.Contains("ยังไม่มียอดขาย", await outsiderPage.Locator(".sales-dashboard").InnerTextAsync());
            var adminPage = await Login(admin); adminPage.PageError += (_, error) => errors.Add(error);
            var overviewResponse = await adminPage.GotoAsync(origin + "/Admin"); Assert.Equal(200, overviewResponse!.Status); Assert.Contains("/Dashboard/Overview", adminPage.Url);
            Assert.Equal(1, await adminPage.Locator("#sales-chart svg").CountAsync());
            Assert.True(await adminPage.Locator(".dash-financial").IsVisibleAsync());
            await adminPage.ScreenshotAsync(new() { Path = Path.Combine(screenshots, "dashboard-overview-desktop.png"), FullPage = true });
            await adminPage.SetViewportSizeAsync(390, 844);
            Assert.True(await adminPage.EvaluateAsync<bool>("document.documentElement.scrollWidth <= window.innerWidth + 1"));
            await adminPage.ScreenshotAsync(new() { Path = Path.Combine(screenshots, "dashboard-overview-mobile.png"), FullPage = true });
            Assert.Equal(200, await adminPage.EvaluateAsync<int>("async () => (await fetch('/Dashboard/ExportOverview')).status"));
            await adminPage.GotoAsync(origin + "/Dashboard/Overview?from=2001-01-01&to=2001-01-01&channel=Shopee");
            Assert.Contains("ยังไม่มียอดขาย", await adminPage.Locator(".sales-dashboard").InnerTextAsync());
            Assert.Empty(errors);
        }
        finally
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync(); await stdout; await stderr;
        }
    }
}
