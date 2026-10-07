using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using AmHerb.Web.Domain;
using AmHerb.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;

namespace AmHerb.Tests;
[Collection("SQL")]
public class MemberStoreBrowserTests(SqlFixture fixture)
{
    [BrowserFact] public async Task Member_store_checkout_management_and_network_render_and_isolate_data()
    {
        await using var h = new Harness(fixture); await h.InitializeAsync(); var owner = await h.MemberAsync(); var outsider = await h.MemberAsync();
        var password = "T!" + Guid.NewGuid().ToString("N") + "a9";
        foreach (var member in new[] { owner, outsider })
        {
            var user = await h.Db.Users.SingleAsync(x => x.Id == member.UserId); user.SecurityStamp = Guid.NewGuid().ToString(); user.PasswordHash = new PasswordHasher<AppUser>().HashPassword(user, password);
        }
        await h.Db.SaveChangesAsync();
        var service = new MemberStoreService(h.Db, h.Members, new AuditService(h.Db, new Actor(new HttpContextAccessor())));
        var store = await service.CreateAsync(owner.UserId); await service.SaveAsync(owner.UserId, "Browser test shop", "ร้านทดสอบ", "0801234567", true); await service.SetProductAsync(owner.UserId, h.SkuId, true);
        await h.Inventory.ReceiveAsync(h.SkuId, store.WarehouseId, "BROWSER", h.Clock.Now.AddDays(-2), h.Clock.Now.AddYears(1), 5, 100, "Browser fixture");
        var root = new DirectoryInfo(AppContext.BaseDirectory); while (root != null && !File.Exists(Path.Combine(root.FullName, "AmHerb.sln"))) root = root.Parent; Assert.NotNull(root);
        var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start(); var port = ((IPEndPoint)listener.LocalEndpoint).Port; listener.Stop(); var origin = "http://127.0.0.1:" + port;
        var info = new ProcessStartInfo("dotnet") { WorkingDirectory = Path.Combine(root!.FullName, "src", "AmHerb.Web"), UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
        info.ArgumentList.Add(typeof(Program).Assembly.Location); info.ArgumentList.Add("--urls"); info.ArgumentList.Add(origin);
        info.Environment["ASPNETCORE_ENVIRONMENT"] = "Testing"; info.Environment["ConnectionStrings__DefaultConnection"] = fixture.Connection; info.Environment["Jobs__Enabled"] = "false"; info.Environment["Logging__LogLevel__Default"] = "Warning";
        using var process = Process.Start(info)!; var stdout = process.StandardOutput.ReadToEndAsync(); var stderr = process.StandardError.ReadToEndAsync();
        try
        {
            using var http = new HttpClient(); var ready = false;
            for (var i = 0; i < 60; i++) { try { if ((await http.GetAsync(origin)).IsSuccessStatusCode) { ready = true; break; } } catch (HttpRequestException) { } await Task.Delay(500); }
            Assert.True(ready);
            using var playwright = await Playwright.CreateAsync(); await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true, ExecutablePath = Environment.GetEnvironmentVariable("AMHERB_CHROME") ?? @"C:\Program Files\Google\Chrome\Application\chrome.exe" });
            var guest = await browser.NewPageAsync(); var errors = new List<string>(); guest.PageError += (_, e) => errors.Add(e);
            Assert.Equal(200, (await guest.GotoAsync(origin + "/s/" + store.Slug))!.Status);
            await guest.Locator("form[action$='/add'] button").ClickAsync(); await guest.WaitForURLAsync("**/cart", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            Assert.Contains("/s/" + store.Slug + "/checkout", await guest.Locator("form:has(#Checkout_Key)").GetAttributeAsync("action"));
            await guest.Locator("#Checkout_CustomerName").FillAsync("Browser Customer"); await guest.Locator("#Checkout_Phone").FillAsync("0801234567"); await guest.Locator("#Checkout_Address").FillAsync("Test shipping address");
            await guest.Locator("form:has(#Checkout_Key) button").ClickAsync(); await guest.WaitForURLAsync("**/Orders/Detail/**", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            Assert.Contains("Browser Customer", await guest.Locator("body").InnerTextAsync());
            h.Db.ChangeTracker.Clear(); var order = await h.Db.Orders.SingleAsync(x => x.StoreId == store.Id); Assert.Equal(store.MemberId, order.SellerMemberId);
            async Task<IPage> Login(Member member)
            {
                var page = await browser.NewPageAsync(); await page.GotoAsync(origin + "/Identity/Account/Login");
                await page.Locator("#Input_Email").FillAsync((await h.Db.Users.SingleAsync(x => x.Id == member.UserId)).Email!); await page.Locator("#Input_Password").FillAsync(password); await page.Locator("#login-submit").ClickAsync(); await page.WaitForURLAsync(url => !url.Contains("/Login", StringComparison.OrdinalIgnoreCase)); return page;
            }
            var ownerPage = await Login(owner);
            foreach (var route in new[] { "/MyStore", "/MyStore/Products", "/MyStore/Orders", "/MyStore/Inventory", "/MyStore/Accounting", "/NetworkTokens", "/Orders/Detail/" + order.PublicId }) Assert.Equal(200, (await ownerPage.GotoAsync(origin + route))!.Status);
            await h.Commerce.ConfirmPaymentAsync(order.Id, "BROWSER-" + Guid.NewGuid(), order.CashPayable, "Test bank confirmation");
            await ownerPage.GotoAsync(origin + "/MyStore/Orders"); await ownerPage.Locator("input[name=carrier]").FillAsync("Test carrier"); await ownerPage.Locator("input[name=tracking]").FillAsync("TRACK123"); await ownerPage.Locator("form[action$='/Ship'] button").ClickAsync();
            h.Db.ChangeTracker.Clear(); Assert.Equal(OrderStatus.Shipped, (await h.Db.Orders.SingleAsync(x => x.Id == order.Id)).Status);
            await ownerPage.GotoAsync(origin + "/MyStore/Accounting"); Assert.Contains("990.00", await ownerPage.Locator("body").InnerTextAsync());
            var outsiderPage = await Login(outsider); Assert.Equal(404, (await outsiderPage.APIRequest.GetAsync(origin + "/Orders/Detail/" + order.PublicId)).Status);
            await ownerPage.GotoAsync(origin + "/NetworkTokens"); await ownerPage.SetViewportSizeAsync(390, 844); Assert.True(await ownerPage.EvaluateAsync<bool>("document.documentElement.scrollWidth <= window.innerWidth + 1"));
            var screenshots = Path.Combine(root.FullName, "artifacts", "screenshots"); Directory.CreateDirectory(screenshots); await ownerPage.ScreenshotAsync(new() { Path = Path.Combine(screenshots, "member-network-mobile.png"), FullPage = true });
            await guest.GotoAsync(origin + "/s/" + store.Slug); await guest.SetViewportSizeAsync(390, 844); Assert.True(await guest.EvaluateAsync<bool>("document.documentElement.scrollWidth <= window.innerWidth + 1")); await guest.ScreenshotAsync(new() { Path = Path.Combine(screenshots, "member-store-mobile.png"), FullPage = true });
            Assert.Empty(errors);
        }
        finally { if (!process.HasExited) process.Kill(true); await process.WaitForExitAsync(); await stdout; await stderr; }
    }
}
