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
            await guest.Locator("#Checkout_CustomerName").FillAsync("Browser Customer"); await guest.Locator("#Checkout_Phone").FillAsync("0801234567"); await CheckoutTestData.Address(guest, selectVillage: true);
            var checkoutScreenshots = Path.Combine(root.FullName, "artifacts", "screenshots"); Directory.CreateDirectory(checkoutScreenshots);
            await guest.ScreenshotAsync(new() { Path = Path.Combine(checkoutScreenshots, "checkout-wizard-desktop.png"), FullPage = true });
            await guest.SetViewportSizeAsync(390, 844);
            Assert.True(await guest.EvaluateAsync<bool>("document.documentElement.scrollWidth <= window.innerWidth + 1"));
            await guest.ScreenshotAsync(new() { Path = Path.Combine(checkoutScreenshots, "checkout-wizard-mobile.png"), FullPage = true });
            await guest.GetByRole(AriaRole.Button, new() { Name = "ยืนยันคำสั่งซื้อ", Exact = true }).ClickAsync(); await guest.WaitForURLAsync("**/Orders/Detail/**", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            Assert.Contains("Browser Customer", await guest.Locator("body").InnerTextAsync());
            h.Db.ChangeTracker.Clear(); var order = await h.Db.Orders.SingleAsync(x => x.StoreId == store.Id); Assert.Equal(store.MemberId, order.SellerMemberId);
            async Task<IPage> Login(Member member)
            {
                var page = await browser.NewPageAsync(); await page.GotoAsync(origin + "/Identity/Account/Login");
                await page.Locator("#Input_Email").FillAsync((await h.Db.Users.SingleAsync(x => x.Id == member.UserId)).Email!); await page.Locator("#Input_Password").FillAsync(password); await page.Locator("#login-submit").ClickAsync(); await page.WaitForURLAsync(url => !url.Contains("/Login", StringComparison.OrdinalIgnoreCase)); return page;
            }
            var ownerPage = await Login(owner);
            if (!await h.Db.Roles.AnyAsync(x => x.Name == "Member")) { h.Db.Roles.Add(new IdentityRole("Member") { NormalizedName = "MEMBER" }); await h.Db.SaveChangesAsync(); }
            var signup = await browser.NewPageAsync();
            await signup.SetViewportSizeAsync(390, 844);
            Assert.Equal(200, (await signup.GotoAsync(origin + "/Account/Join"))!.Status);
            await signup.ScreenshotAsync(new() { Path = Path.Combine(checkoutScreenshots, "registration-guide-mobile.png"), FullPage = true });
            await signup.GotoAsync(origin + "/r/" + owner.ReferralCode);
            await signup.GotoAsync(origin + "/Account/Register");
            Assert.Equal(owner.ReferralCode, await signup.Locator("#SponsorCode").InputValueAsync());
            await signup.GotoAsync(origin + "/Account/Register?withoutSponsor=true");
            Assert.Equal("", await signup.Locator("#SponsorCode").InputValueAsync());
            await signup.GotoAsync(origin + "/Account/Register?sponsorCode=invalid-referral");
            await Assertions.Expect(signup.Locator("#register-submit")).ToBeDisabledAsync();
            await signup.GotoAsync(origin + "/Account/Register?sponsorCode=" + owner.ReferralCode);
            var signupEmail = Guid.NewGuid().ToString("N") + "@example.invalid";
            await signup.Locator("#Name").FillAsync("สมาชิกจากวิซาร์ด"); await signup.Locator("#Email").FillAsync(signupEmail);
            await signup.Locator("[data-register-next]").ClickAsync();
            await signup.Locator("#Password").FillAsync("abcde"); await signup.Locator("#ConfirmPassword").FillAsync("wrong");
            await signup.Locator("[data-register-next]").ClickAsync();
            await Assertions.Expect(signup.Locator("#ConfirmPassword")).ToBeVisibleAsync();
            await signup.Locator("#ConfirmPassword").FillAsync("abcde");
            await signup.Locator("#show-register-password").CheckAsync();
            Assert.Equal("text", await signup.Locator("#Password").GetAttributeAsync("type"));
            await signup.Locator("[data-register-next]").ClickAsync();
            await Assertions.Expect(signup.Locator("[data-register-step]").Nth(2)).ToContainTextAsync(owner.Name);
            Assert.DoesNotContain("abcde", await signup.Locator("[data-register-step]").Nth(2).InnerTextAsync());
            Assert.True(await signup.EvaluateAsync<bool>("document.documentElement.scrollWidth <= innerWidth + 1"));
            await signup.ScreenshotAsync(new() { Path = Path.Combine(checkoutScreenshots, "registration-review-mobile.png"), FullPage = true });
            await signup.Locator("#register-submit").ClickAsync(); await signup.WaitForURLAsync("**/Identity/Account/Login");
            h.Db.ChangeTracker.Clear();
            var registeredUser = await h.Db.Users.SingleAsync(x => x.Email == signupEmail);
            Assert.False(registeredUser.EmailConfirmed);
            Assert.Equal(owner.Id, (await h.Db.Members.SingleAsync(x => x.UserId == registeredUser.Id)).SponsorMemberId);
            registeredUser.EmailConfirmed = true; await h.Db.SaveChangesAsync();
            await signup.Locator("#Input_Email").FillAsync(signupEmail); await signup.Locator("#Input_Password").FillAsync("abcde");
            await signup.Locator("#login-submit").ClickAsync(); await signup.WaitForURLAsync(url => !url.Contains("/Login"));
            await signup.GotoAsync(origin + "/Account/Register?sponsorCode=" + outsider.ReferralCode);
            Assert.EndsWith("/Account/Join", signup.Url);
            Assert.Equal(200, (await signup.GotoAsync(origin + "/Identity/Account/Manage/ChangePassword"))!.Status);
            await signup.Locator("#Input_OldPassword").FillAsync("abcde");
            await signup.Locator("#Input_Password").FillAsync("abcdefghij"); await signup.Locator("#Input_ConfirmPassword").FillAsync("abcdefghij");
            await signup.GetByRole(AriaRole.Button, new() { Name = "บันทึกรหัสผ่าน" }).ClickAsync(); await signup.WaitForURLAsync("**/Member#profile");
            h.Db.ChangeTracker.Clear(); registeredUser = await h.Db.Users.SingleAsync(x => x.Email == signupEmail);
            Assert.NotEqual(PasswordVerificationResult.Failed, new PasswordHasher<AppUser>().VerifyHashedPassword(registeredUser, registeredUser.PasswordHash!, "abcdefghij"));
            var resetPage = await browser.NewPageAsync();
            Assert.Equal(200, (await resetPage.GotoAsync(origin + "/Identity/Account/ResetPassword?code=aW52YWxpZA"))!.Status);
            await resetPage.Locator("#Input_Email").FillAsync(signupEmail); await resetPage.Locator("#Input_Password").FillAsync("12345"); await resetPage.Locator("#Input_ConfirmPassword").FillAsync("12345");
            await resetPage.GetByRole(AriaRole.Button, new() { Name = "บันทึกรหัสผ่าน" }).ClickAsync();
            await Assertions.Expect(resetPage.Locator(".validation-summary-errors")).ToContainTextAsync("Invalid token");
            await signup.CloseAsync(); await resetPage.CloseAsync();
            foreach (var route in new[] { "/MyStore", "/MyStore/Products", "/MyStore/Orders", "/MyStore/Inventory", "/MyStore/Accounting", "/NetworkTokens", "/Orders/Detail/" + order.PublicId }) Assert.Equal(200, (await ownerPage.GotoAsync(origin + route))!.Status);
            await guest.Locator("#payment-reference").FillAsync("BROWSER-SLIP");
            await guest.Locator("[data-slip-file]").SetInputFilesAsync(CheckoutTestData.SlipFile);
            await guest.GetByRole(AriaRole.Button, new() { Name = "ส่งข้อมูลแจ้งโอน" }).ClickAsync();
            await Assertions.Expect(guest.Locator("[data-live-region=slips]")).ToContainTextAsync("BROWSER-SLIP");
            await guest.GetByRole(AriaRole.Button, new() { Name = "ดูภาพสลิป" }).ClickAsync();
            await Assertions.Expect(guest.Locator("#image-modal")).ToBeVisibleAsync();
            await guest.ScreenshotAsync(new() { Path = Path.Combine(checkoutScreenshots, "slip-popup-mobile.png"), FullPage = true });
            await guest.Locator("#image-modal .btn-close").ClickAsync();
            h.Db.ChangeTracker.Clear();
            await h.Commerce.ConfirmPaymentAsync(order.Id, "BROWSER-" + Guid.NewGuid(), order.CashPayable, "Test bank confirmation");
            await ownerPage.GotoAsync(origin + "/MyStore/Orders");
            await ownerPage.GetByRole(AriaRole.Button, new() { Name = "เริ่มเตรียมสินค้า" }).ClickAsync();
            await Assertions.Expect(ownerPage.Locator("[data-live-region=store-orders]")).ToContainTextAsync("กำลังเตรียมสินค้า");
            await ownerPage.Locator("select[name=carrier]").SelectOptionAsync(new SelectOptionValue { Label = "ไปรษณีย์ไทย" }); await ownerPage.Locator("input[name=tracking]").FillAsync("TRACK123"); await ownerPage.Locator("form[action$='/Ship'] button").ClickAsync();
            h.Db.ChangeTracker.Clear(); Assert.Equal(OrderStatus.Shipped, (await h.Db.Orders.SingleAsync(x => x.Id == order.Id)).Status);
            await ownerPage.SetViewportSizeAsync(390, 844);
            Assert.True(await ownerPage.EvaluateAsync<bool>("document.documentElement.scrollWidth <= window.innerWidth + 1"));
            await ownerPage.ScreenshotAsync(new() { Path = Path.Combine(checkoutScreenshots, "store-shipping-mobile.png"), FullPage = true });
            await ownerPage.GetByRole(AriaRole.Button, new() { Name = "ยืนยันส่งมอบแล้ว" }).ClickAsync();
            await Assertions.Expect(ownerPage.Locator("[data-live-region=store-orders]")).ToContainTextAsync("ส่งมอบแล้ว");
            await ownerPage.GotoAsync(origin + "/MyStore/Accounting"); Assert.Contains("990.00", await ownerPage.Locator("body").InnerTextAsync());
            var outsiderPage = await Login(outsider); Assert.Equal(404, await outsiderPage.EvaluateAsync<int>("async url => (await fetch(url)).status", origin + "/Orders/Detail/" + order.PublicId));
            Assert.Equal(404, await outsiderPage.EvaluateAsync<int>("async url => (await fetch(url)).status", origin + "/MyStore/ShippingLabel/" + order.Id));
            Assert.Equal(200, (await ownerPage.GotoAsync(origin + "/MyStore/ShippingLabel/" + order.Id))!.Status);
            await Assertions.Expect(ownerPage.Locator(".recipient")).ToContainTextAsync("Browser Customer");
            await ownerPage.Locator("summary").ClickAsync();
            await ownerPage.Locator("#sender-address").FillAsync("99 ถนนทดสอบ แขวงพระบรมมหาราชวัง เขตพระนคร กรุงเทพมหานคร 10200");
            await Assertions.Expect(ownerPage.Locator("[data-sender=address]")).ToContainTextAsync("10200");
            await ownerPage.Locator("#sender-postcode").FillAsync("1020");
            await ownerPage.Locator("#print-label").ClickAsync();
            await Assertions.Expect(ownerPage.Locator("#print-error")).ToContainTextAsync("5 หลัก");
            await ownerPage.Locator("#sender-postcode").FillAsync("10200");
            await Assertions.Expect(ownerPage.Locator("[data-sender=postcode]")).ToHaveTextAsync("10200");
            await ownerPage.EvaluateAsync("window.print = () => { window.didPrint = true; }");
            await ownerPage.Locator("#print-label").ClickAsync();
            Assert.True(await ownerPage.EvaluateAsync<bool>("window.didPrint === true"));
            var pdf = await ownerPage.PdfAsync(new() { PreferCSSPageSize = true, Path = Path.Combine(checkoutScreenshots, "shipping-label-a5.pdf") });
            var pdfText = System.Text.Encoding.Latin1.GetString(pdf);
            var media = System.Text.RegularExpressions.Regex.Match(pdfText, @"/MediaBox\s*\[0 0 ([\d.]+) ([\d.]+)\]");
            Assert.True(media.Success);
            Assert.InRange(double.Parse(media.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture), 418, 421);
            Assert.InRange(double.Parse(media.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture), 594, 597);
            Assert.Single(System.Text.RegularExpressions.Regex.Matches(pdfText, @"/Type /Page\b"));
            Assert.True(await ownerPage.EvaluateAsync<bool>("document.documentElement.scrollWidth <= window.innerWidth + 1"));
            await ownerPage.ScreenshotAsync(new() { Path = Path.Combine(checkoutScreenshots, "shipping-label-mobile.png"), FullPage = true });
            await ownerPage.Locator("#sender-address").FillAsync("");
            await ownerPage.Locator("#print-label").ClickAsync();
            await Assertions.Expect(ownerPage.Locator("#print-error")).ToBeVisibleAsync();
            await ownerPage.GotoAsync(origin + "/Member");
            foreach (var guideKey in new[] { "store", "buy", "pos", "ship", "token", "team" })
            {
                await ownerPage.Locator($"[data-member-guide={guideKey}]").ClickAsync();
                await Assertions.Expect(ownerPage.Locator("#guide-progress")).ToContainTextAsync("ขั้นตอน 1");
                await ownerPage.Locator("#guide-next").ClickAsync();
                await Assertions.Expect(ownerPage.Locator("#guide-progress")).ToContainTextAsync("ขั้นตอน 2");
                await ownerPage.Locator("#guide-back").ClickAsync();
                await Assertions.Expect(ownerPage.Locator("#guide-back")).ToBeDisabledAsync();
                await ownerPage.Locator("#member-guide .btn-close").ClickAsync();
                await Assertions.Expect(ownerPage.Locator("#member-guide")).ToBeHiddenAsync();
            }
            Assert.True(await ownerPage.EvaluateAsync<bool>("document.documentElement.scrollWidth <= window.innerWidth + 1"));
            await ownerPage.ScreenshotAsync(new() { Path = Path.Combine(checkoutScreenshots, "member-guide-dashboard-mobile.png"), FullPage = true });
            var slipId = await h.Db.PaymentSlips.Where(x => x.OrderId == order.Id).Select(x => x.Id).SingleAsync();
            Assert.Equal(404, await outsiderPage.EvaluateAsync<int>("async url => (await fetch(url)).status", origin + "/Orders/Slip/" + slipId));
            Assert.Equal(200, await ownerPage.EvaluateAsync<int>("async url => (await fetch(url)).status", origin + "/Orders/Slip/" + slipId));
            Assert.Equal(200, await guest.EvaluateAsync<int>("async url => (await fetch(url)).status", origin + "/Orders/Slip/" + slipId));
            await ownerPage.GotoAsync(origin + "/NetworkTokens"); await ownerPage.SetViewportSizeAsync(390, 844); Assert.True(await ownerPage.EvaluateAsync<bool>("document.documentElement.scrollWidth <= window.innerWidth + 1"));
            var screenshots = Path.Combine(root.FullName, "artifacts", "screenshots"); Directory.CreateDirectory(screenshots); await ownerPage.ScreenshotAsync(new() { Path = Path.Combine(screenshots, "member-network-mobile.png"), FullPage = true });
            await guest.GotoAsync(origin + "/s/" + store.Slug); await guest.SetViewportSizeAsync(390, 844); Assert.True(await guest.EvaluateAsync<bool>("document.documentElement.scrollWidth <= window.innerWidth + 1")); await guest.ScreenshotAsync(new() { Path = Path.Combine(screenshots, "member-store-mobile.png"), FullPage = true });
            Assert.Empty(errors);
        }
        finally { if (!process.HasExited) process.Kill(true); await process.WaitForExitAsync(); var log = await stdout; Console.WriteLine(log.Length > 12000 ? log[^12000..] : log); Console.WriteLine(await stderr); }
    }
}
