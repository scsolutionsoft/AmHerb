using AmHerb.Web.Services;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using AmHerb.Web.Data;
using AmHerb.Web.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;

namespace AmHerb.Tests;
public class BrowserFactAttribute : FactAttribute
{
    public BrowserFactAttribute()
    { if (Environment.GetEnvironmentVariable("AMHERB_RUN_SQL_TESTS") != "1" || Environment.GetEnvironmentVariable("AMHERB_RUN_BROWSER_TESTS") != "1") Skip = "Opt in with AMHERB_RUN_SQL_TESTS=1 and AMHERB_RUN_BROWSER_TESTS=1; requires Chrome."; }
}
[Collection("SQL")]
public class BrowserTests(SqlFixture fixture)
{
    [BrowserFact] public async Task Store_member_admin_and_user_POS_work_in_desktop_and_mobile_browser()
    {
        await using var h = new Harness(fixture); await h.InitializeAsync();
        await h.Inventory.ReceiveAsync(h.SkuId, 1, "WEB", h.Clock.Now.AddDays(-30), h.Clock.Now.AddYears(1), 10, 100, "Browser store stock");
        var cashier = await h.MemberAsync(); var outsider = await h.MemberAsync(); var admin = await h.MemberAsync();
        var password = "T!" + Guid.NewGuid().ToString("N") + "a9";
        foreach (var member in new[] { cashier, outsider, admin })
        {
            var user = await h.Db.Users.SingleAsync(x => x.Id == member.UserId); user.SecurityStamp = Guid.NewGuid().ToString(); user.PasswordHash = new PasswordHasher<AppUser>().HashPassword(user, password);
        }
        foreach (var role in SeedData.Roles) if (!await h.Db.Roles.AnyAsync(x => x.Name == role)) h.Db.Roles.Add(new IdentityRole(role) { NormalizedName = role.ToUpperInvariant() });
        await h.Db.SaveChangesAsync();
        var adminRole = await h.Db.Roles.SingleAsync(x => x.Name == "SuperAdmin"); h.Db.UserRoles.Add(new IdentityUserRole<string> { UserId = admin.UserId, RoleId = adminRole.Id }); await h.Db.SaveChangesAsync();
        var root = new DirectoryInfo(AppContext.BaseDirectory); while (root != null && !File.Exists(Path.Combine(root.FullName, "AmHerb.sln"))) root = root.Parent;
        Assert.NotNull(root); var project = Path.Combine(root!.FullName, "src", "AmHerb.Web");
        var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start(); var port = ((IPEndPoint)listener.LocalEndpoint).Port; listener.Stop();
        var origin = "http://127.0.0.1:" + port;
        var processInfo = new ProcessStartInfo("dotnet") { WorkingDirectory = project, UseShellExecute = false, RedirectStandardError = true, RedirectStandardOutput = true, CreateNoWindow = true };
        processInfo.ArgumentList.Add(typeof(Program).Assembly.Location); processInfo.ArgumentList.Add("--urls"); processInfo.ArgumentList.Add(origin);
        processInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Testing"; processInfo.Environment["ConnectionStrings__DefaultConnection"] = fixture.Connection;
        processInfo.Environment["Jobs__Enabled"] = "false"; processInfo.Environment["Logging__LogLevel__Default"] = "Warning";
        using var process = Process.Start(processInfo)!; var output = process.StandardOutput.ReadToEndAsync(); var errors = process.StandardError.ReadToEndAsync();
        try
        {
            using var client = new HttpClient(); var ready = false;
            for (var attempt = 0; attempt < 60; attempt++)
            { try { if ((await client.GetAsync(origin)).IsSuccessStatusCode) { ready = true; break; } } catch (HttpRequestException) { } await Task.Delay(500); }
            Assert.True(ready, "Test web server failed to start.");
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(origin + "/api/v1/token/balance")).StatusCode);
            using var playwright = await Playwright.CreateAsync();
            var chrome = Environment.GetEnvironmentVariable("AMHERB_CHROME") ?? @"C:\Program Files\Google\Chrome\Application\chrome.exe";
            await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true, ExecutablePath = chrome });
            var context = await browser.NewContextAsync(new() { ViewportSize = new() { Width = 1440, Height = 1000 } });
            var page = await context.NewPageAsync(); page.SetDefaultTimeout(15000);
            var jsErrors = new List<string>(); page.PageError += (_, e) => jsErrors.Add(e);
            var screenshotDirectory = Path.Combine(root.FullName, "artifacts", "screenshots"); Directory.CreateDirectory(screenshotDirectory);
            var attachmentDirectory=Path.Combine(root.FullName,"artifacts","import");
            if(Directory.Exists(attachmentDirectory)){
                foreach(var slot in new[]{2,3,4}){
                    var photo=Directory.GetFiles(attachmentDirectory,"attached-"+slot+".*").FirstOrDefault();
                    if(photo!=null&&!await h.Db.MediaAssets.AnyAsync(x=>x.Purpose=="brand"&&x.Slot==slot)){
                        var bytes=await File.ReadAllBytesAsync(photo);h.Db.MediaAssets.Add(new MediaAsset{Purpose="brand",Slot=slot,Data=bytes,ContentType=MediaService.Validate(bytes),Alt="AM HERB"});
                    }
                }
                await h.Db.SaveChangesAsync();
            }
            var productDirectory=Path.Combine(root.FullName,"artifacts","product-import");
            if(Directory.Exists(productDirectory))
            {
                var pricesBefore=await h.Db.ProductPrices.AsNoTracking().OrderBy(x=>x.Id).Select(x=>x.Amount).ToListAsync();
                var tokensBefore=await h.Db.TokenRates.AsNoTracking().OrderBy(x=>x.Id).Select(x=>x.BaseToken).ToListAsync();
                var audit=new AuditService(h.Db,new Actor(new Microsoft.AspNetCore.Http.HttpContextAccessor()));
                await ProductArtworkImport.RunAsync(h.Db,audit,productDirectory);
                var count=await h.Db.MediaAssets.CountAsync();
                await ProductArtworkImport.RunAsync(h.Db,audit,productDirectory);
                Assert.Equal(count,await h.Db.MediaAssets.CountAsync());
                Assert.Equal(pricesBefore,await h.Db.ProductPrices.AsNoTracking().OrderBy(x=>x.Id).Select(x=>x.Amount).ToListAsync());
                Assert.Equal(tokensBefore,await h.Db.TokenRates.AsNoTracking().OrderBy(x=>x.Id).Select(x=>x.BaseToken).ToListAsync());
                var draft=await h.Db.ContentPosts.SingleAsync(x=>x.Title=="ร่าง: แผนสมาชิก AM HERB — ตรวจสอบข้อมูล Token");
                Assert.False(draft.Published);
            }
            await page.GotoAsync(origin); await page.EvaluateAsync("document.fonts.ready");
            await page.ScreenshotAsync(new() { Path = Path.Combine(screenshotDirectory, "home-desktop.png"), FullPage = true });
            await page.SetViewportSizeAsync(390,844);
            Assert.True(await page.EvaluateAsync<bool>("document.documentElement.scrollWidth <= window.innerWidth + 1"));
            await page.ScreenshotAsync(new(){Path=Path.Combine(screenshotDirectory,"home-mobile.png"),FullPage=true});
            await page.GotoAsync(origin+"/Catalog");
            Assert.True(await page.EvaluateAsync<bool>("document.documentElement.scrollWidth <= window.innerWidth + 1"));
            await page.ScreenshotAsync(new(){Path=Path.Combine(screenshotDirectory,"catalog-mobile.png"),FullPage=true});
            await page.SetViewportSizeAsync(1440,1000);
            await page.ScreenshotAsync(new(){Path=Path.Combine(screenshotDirectory,"catalog-desktop.png"),FullPage=true});
            if(Directory.Exists(productDirectory))
            {
                foreach(var code in ProductArtworkImport.Mapping.Select(x=>x.Code).Distinct())
                {
                    var sku=await h.Db.Skus.SingleAsync(x=>x.Code==code);
                    await page.GotoAsync(origin+"/Catalog/Detail/"+sku.Id);
                    await page.Locator(".gallery-main").ClickAsync();
                    await page.Locator("#image-modal").WaitForAsync(new(){State=WaitForSelectorState.Visible});
                    await page.WaitForFunctionAsync("document.querySelector('#image-modal-image').naturalWidth > 0");
                    if(code=="PHYTOSYNC")await page.ScreenshotAsync(new(){Path=Path.Combine(screenshotDirectory,"product-image-popup.png"),FullPage=true});
                }
            }
            await page.GotoAsync(origin + "/Catalog"); Assert.True(await page.Locator(".product-card").CountAsync() >= 9);
            await page.GotoAsync(origin + "/Catalog/Detail/" + h.SkuId); await page.GetByRole(AriaRole.Button, new() { Name = "เพิ่มลงตะกร้า", Exact = true }).ClickAsync();
            await page.Locator("#Checkout_CustomerName").FillAsync("Guest Customer"); await page.Locator("#Checkout_Phone").FillAsync("0901111111"); await page.Locator("#Checkout_Email").FillAsync("guest@example.invalid"); await page.Locator("#Checkout_Address").FillAsync("Bangkok test address");
            await page.GetByRole(AriaRole.Button, new() { Name = "ยืนยันคำสั่งซื้อ", Exact = true }).ClickAsync(); await page.WaitForURLAsync("**/Orders/Detail/*");
            Assert.Contains("PendingPayment", await page.Locator(".receipt").InnerTextAsync());
            async Task Login(IPage target, Member member)
            {
                var email = (await h.Db.Users.SingleAsync(x => x.Id == member.UserId)).Email!;
                await target.GotoAsync(origin + "/Identity/Account/Login");
                await target.Locator("#Input_Email").FillAsync(email); await target.Locator("#Input_Password").FillAsync(password); await target.Locator("#login-submit").ClickAsync();
                await target.WaitForURLAsync(url => !url.Contains("/Login", StringComparison.OrdinalIgnoreCase));
            }
            await Login(page, cashier); await page.GotoAsync(origin + "/Member"); Assert.True(await page.Locator("#wallet").IsVisibleAsync());
            await page.ScreenshotAsync(new() { Path = Path.Combine(screenshotDirectory, "member.png"), FullPage = true });
            await page.GotoAsync(origin + "/Pos"); await page.Locator("#opening").FillAsync("500"); await page.GetByRole(AriaRole.Button, new() { Name = "เปิดกะ", Exact = true }).ClickAsync();
            await page.Locator("#sku-" + h.SkuId).FillAsync("1"); await page.Locator("#phone").FillAsync("0912345678"); await page.Locator("#tendered").FillAsync("1000");
            await page.ScreenshotAsync(new() { Path = Path.Combine(screenshotDirectory, "pos-desktop.png"), FullPage = true });
            await page.GetByRole(AriaRole.Button, new() { Name = "รับชำระ / ออกใบเสร็จ", Exact = true }).ClickAsync(); await page.WaitForURLAsync("**/Orders/Detail/*");
            var receiptUrl = page.Url; Assert.Contains("10.00", await page.Locator(".receipt").InnerTextAsync()); await page.ScreenshotAsync(new() { Path = Path.Combine(screenshotDirectory, "pos-receipt.png"), FullPage = true });
            var otherContext = await browser.NewContextAsync(); var otherPage = await otherContext.NewPageAsync(); await Login(otherPage, outsider);
            var forbiddenReceipt = await otherPage.EvaluateAsync<int>("async url => (await fetch(url, {credentials:'include'})).status", receiptUrl); Assert.Equal(404, forbiddenReceipt);
            var forbiddenAdmin = await otherPage.GotoAsync(origin + "/Admin"); Assert.Contains("AccessDenied", otherPage.Url);
            // Anti-forgery prevents POSTs with the authentication cookie but no request token.
            var noCsrf = await otherPage.EvaluateAsync<int>("async () => (await fetch('/Pos/Open', {method:'POST', credentials:'include', redirect:'manual'})).status"); Assert.Equal(400, noCsrf);
            var adminContext = await browser.NewContextAsync(); var adminPage = await adminContext.NewPageAsync(); await Login(adminPage, admin);
            foreach (var section in new[] { "Dashboard", "Members", "Products", "Prices", "TokenRates", "TokenPolicies", "Ledger", "Orders", "Payments", "POS", "Imports", "Inventory", "Returns", "Promotions", "Reports", "AuditLogs", "Settings" })
            { var response = await adminPage.GotoAsync(origin + "/Admin?section=" + section); Assert.Equal(200, response!.Status); }
            await adminPage.GotoAsync(origin + "/Admin"); await adminPage.ScreenshotAsync(new() { Path = Path.Combine(screenshotDirectory, "admin-dashboard.png"), FullPage = true });
            var stockResponse = await adminPage.GotoAsync(origin + "/Stock"); Assert.Equal(200, stockResponse!.Status);
            Assert.True(await adminPage.Locator(".stock-metrics").IsVisibleAsync());
            await adminPage.ScreenshotAsync(new() { Path = Path.Combine(screenshotDirectory, "stock-desktop.png"), FullPage = true });
            await adminPage.SetViewportSizeAsync(390, 844);
            Assert.True(await adminPage.EvaluateAsync<bool>("document.documentElement.scrollWidth <= window.innerWidth + 1"));
            await adminPage.ScreenshotAsync(new() { Path = Path.Combine(screenshotDirectory, "stock-mobile.png"), FullPage = true });
            await otherPage.GotoAsync(origin + "/Stock?warehouseId=" + h.WarehouseId);
            Assert.Equal(0, await otherPage.Locator(".stock-table tbody tr td strong").CountAsync());
            await page.SetViewportSizeAsync(390, 844); await page.GotoAsync(origin + "/Pos");
            Assert.True(await page.EvaluateAsync<bool>("document.documentElement.scrollWidth <= window.innerWidth + 1"));
            await page.ScreenshotAsync(new() { Path = Path.Combine(screenshotDirectory, "pos-mobile.png"), FullPage = true }); Assert.Empty(jsErrors);
            await page.GotoAsync(origin + "/Transfers");
            var privateWarehouse = await h.Db.Warehouses.SingleAsync(x => x.OwnerMemberId == cashier.Id);
            await page.Locator("#transfer-source").SelectOptionAsync("1");
            await page.Locator("#transfer-destination").SelectOptionAsync(privateWarehouse.Id.ToString());
            await page.Locator("#transfer-sku").SelectOptionAsync(h.SkuId.ToString());
            await page.Locator("#transfer-quantity").FillAsync("2");
            await page.Locator("#transfer-reason").FillAsync("Browser replenishment");
            await page.GetByRole(AriaRole.Button, new() { Name = "ส่งคำขอเบิก", Exact = true }).ClickAsync();
            await page.WaitForURLAsync("**/Transfers/Detail/*"); var transferUrl = page.Url;
            var hiddenTransfer = await otherPage.EvaluateAsync<int>("async url => (await fetch(url, {credentials:'include'})).status", transferUrl); Assert.Equal(404, hiddenTransfer);
            await adminPage.GotoAsync(transferUrl);
            await adminPage.Locator("#dispatch-tracking").FillAsync("BROWSER-TRANSFER");
            await adminPage.Locator("#dispatch-reason").FillAsync("Approved warehouse issue");
            await adminPage.GetByRole(AriaRole.Button, new() { Name = "อนุมัติและจ่ายสินค้า", Exact = true }).ClickAsync();
            await page.GotoAsync(transferUrl);
            await page.Locator("#receive-reason").FillAsync("Received complete");
            await page.GetByRole(AriaRole.Button, new() { Name = "ยืนยันรับครบ 2 ชิ้น", Exact = true }).ClickAsync();
            Assert.Contains("รับเข้าคลังแล้ว", await page.Locator(".page-heading").InnerTextAsync());
            Assert.Equal(2, await h.Db.InventoryBatches.Where(x => x.WarehouseId == privateWarehouse.Id && x.SkuId == h.SkuId).SumAsync(x => x.QtyAvailable));
            Assert.True(await page.EvaluateAsync<bool>("document.documentElement.scrollWidth <= window.innerWidth + 1"));
            await page.ScreenshotAsync(new() { Path = Path.Combine(screenshotDirectory, "transfer-mobile.png"), FullPage = true });
            await adminPage.SetViewportSizeAsync(1440,1000);
            var productId = (await h.Db.Skus.FindAsync(h.SkuId))!.ProductId;
            await adminPage.GotoAsync(origin + "/Media/Product/" + productId);
            await adminPage.Locator("#file-1").SetInputFilesAsync(new FilePayload { Name="product.png", MimeType="image/png", Buffer=EngagementTests.Png });
            await adminPage.Locator("[data-local-preview]").First.WaitForAsync(new(){State=WaitForSelectorState.Visible});
            await adminPage.Locator("form[action='/Media/Upload']").First.Locator("button").ClickAsync();
            await adminPage.GotoAsync(origin+"/Admin/Product/"+h.SkuId);
            await adminPage.Locator("#Description").FillAsync("รายละเอียดสินค้าทดสอบที่แก้ไข");
            await adminPage.Locator("#Reason").FillAsync("Verify integrated product editor");
            await adminPage.GetByRole(AriaRole.Button,new(){Name="บันทึกสินค้า",Exact=true}).ClickAsync();
            Assert.Equal("รายละเอียดสินค้าทดสอบที่แก้ไข",await adminPage.Locator("#Description").InputValueAsync());
            await adminPage.Locator("#caption-1").FillAsync("คำอธิบายใหม่");
            await adminPage.Locator("form[action='/Media/Caption']").First.Locator("button").ClickAsync();
            Assert.Equal("คำอธิบายใหม่",await adminPage.Locator("#caption-1").InputValueAsync());
            await adminPage.Locator("#position-1").SelectOptionAsync("2");
            await adminPage.Locator("form[action='/Media/Move']").First.Locator("button").ClickAsync();
            Assert.Equal("คำอธิบายใหม่",await adminPage.Locator("#caption-2").InputValueAsync());
            await adminPage.ScreenshotAsync(new(){Path=Path.Combine(screenshotDirectory,"product-editor-desktop.png"),FullPage=true});
            await adminPage.SetViewportSizeAsync(390,844);
            Assert.True(await adminPage.EvaluateAsync<bool>("document.documentElement.scrollWidth <= window.innerWidth + 1"));
            await adminPage.ScreenshotAsync(new(){Path=Path.Combine(screenshotDirectory,"product-editor-mobile.png"),FullPage=true});
            await adminPage.SetViewportSizeAsync(1440,1000);
            await page.GotoAsync(origin + "/Catalog/Detail/" + h.SkuId);
            await page.Locator(".gallery-main").ClickAsync();
            await page.Locator("#image-modal").WaitForAsync(new(){State=WaitForSelectorState.Visible});
            await page.Locator("#image-modal .btn-close").ClickAsync();
            await adminPage.GotoAsync(origin + "/News/Edit");
            await adminPage.Locator("#news-title").FillAsync("Member campaign browser test");
            await adminPage.Locator("#news-audience").SelectOptionAsync("Members");
            await adminPage.Locator("#news-kind").SelectOptionAsync("Advertisement");
            await adminPage.Locator("#news-body").FillAsync("โปรโมชั่นเฉพาะสมาชิก");
            await adminPage.Locator("input[name='published']").CheckAsync();
            await adminPage.Locator("#news-reason").FillAsync("Browser publication test");
            await adminPage.GetByRole(AriaRole.Button,new(){Name="บันทึกประกาศ",Exact=true}).ClickAsync();
            await page.GotoAsync(origin + "/News");
            Assert.Contains("Member campaign browser test",await page.Locator("main").InnerTextAsync());
            var guestContext = await browser.NewContextAsync();var guestPage=await guestContext.NewPageAsync();
            await guestPage.GotoAsync(origin + "/News");Assert.DoesNotContain("Member campaign browser test",await guestPage.Locator("main").InnerTextAsync());
            await guestPage.GotoAsync(origin + "/Identity/Account/Login");
            await guestPage.ScreenshotAsync(new(){Path=Path.Combine(screenshotDirectory,"login-desktop.png"),FullPage=true});
            await guestPage.SetViewportSizeAsync(390,844);
            Assert.True(await guestPage.EvaluateAsync<bool>("document.documentElement.scrollWidth <= window.innerWidth + 1"));
            await guestPage.ScreenshotAsync(new(){Path=Path.Combine(screenshotDirectory,"login-mobile.png"),FullPage=true});
            await page.GotoAsync(origin + "/Messages");await page.Locator("details summary").ClickAsync();
            await page.Locator("#chat-subject").FillAsync("Browser member support");await page.Locator("#chat-body").FillAsync("สอบถามโปรโมชั่น");
            await page.GetByRole(AriaRole.Button,new(){Name="ส่งข้อความ",Exact=true}).ClickAsync();
            await page.WaitForURLAsync("**/Messages/Detail/*");var conversationUrl=page.Url;
            Assert.Equal(404,await otherPage.EvaluateAsync<int>("async url => (await fetch(url,{credentials:'include'})).status",conversationUrl));
            await adminPage.GotoAsync(conversationUrl);await adminPage.Locator("#reply-body").FillAsync("เจ้าหน้าที่ตอบกลับแล้ว");
            await adminPage.GetByRole(AriaRole.Button,new(){Name="ส่งคำตอบ",Exact=true}).ClickAsync();
            await page.GotoAsync(conversationUrl);Assert.Contains("เจ้าหน้าที่ตอบกลับแล้ว",await page.Locator(".chat-thread").InnerTextAsync());
            await page.ScreenshotAsync(new(){Path=Path.Combine(screenshotDirectory,"messages-mobile.png"),FullPage=true});
            await adminPage.GotoAsync(origin + "/Accounting");
            await adminPage.Locator("[data-print]").ClickAsync();await adminPage.Locator("#report-modal").WaitForAsync(new(){State=WaitForSelectorState.Visible});
            Assert.True(await adminPage.Locator(".report-preview table").CountAsync()>0);
            await adminPage.ScreenshotAsync(new(){Path=Path.Combine(screenshotDirectory,"accounting-preview.png"),FullPage=true});
            await adminPage.GotoAsync(origin+"/Credit");
            await adminPage.Locator("details summary").ClickAsync();
            await adminPage.Locator("#credit-code").FillAsync(cashier.Code);
            await adminPage.Locator("#credit-limit").FillAsync("5000");
            await adminPage.Locator("#credit-reason").FillAsync("Credit browser approval");
            await adminPage.GetByRole(AriaRole.Button,new(){Name="อนุมัติวงเงิน",Exact=true}).ClickAsync();
            await adminPage.WaitForURLAsync("**/Credit/Account/*");var accountUrl=adminPage.Url;
            Assert.Equal(404,await otherPage.EvaluateAsync<int>("async url => (await fetch(url,{credentials:'include'})).status",accountUrl));
            await page.GotoAsync(origin+"/Catalog/Detail/"+h.SkuId);
            await page.GetByRole(AriaRole.Button,new(){Name="เพิ่มลงตะกร้า",Exact=true}).First.ClickAsync();
            await page.Locator("#Checkout_CustomerName").FillAsync("Credit member");
            await page.Locator("#Checkout_Phone").FillAsync("0901239999");
            await page.Locator("#Checkout_Address").FillAsync("Credit test address");
            await page.Locator("#Checkout_UseCredit").CheckAsync();
            await page.GetByRole(AriaRole.Button,new(){Name="ยืนยันคำสั่งซื้อ",Exact=true}).ClickAsync();
            await page.WaitForURLAsync("**/Orders/Detail/*");
            Assert.Contains("รายการซื้อด้วยเครดิตบริษัท",await page.Locator("main").InnerTextAsync());
            await page.GotoAsync(accountUrl);
            await page.Locator("#repay-amount").FillAsync("100");
            await page.Locator("#repay-ref").FillAsync("BROWSER-"+Guid.NewGuid());
            await page.Locator("#repay-slip").SetInputFilesAsync(new FilePayload{Name="slip.png",MimeType="image/png",Buffer=EngagementTests.Png});
            await page.GetByRole(AriaRole.Button,new(){Name="แจ้งโอนชำระเครดิต",Exact=true}).ClickAsync();
            var slipUrl=await page.GetByRole(AriaRole.Link,new(){Name="ดูสลิปโอนเงิน ↗",Exact=true}).GetAttributeAsync("href");
            Assert.Equal(404,await otherPage.EvaluateAsync<int>("async url => (await fetch(url,{credentials:'include'})).status",origin+slipUrl));
            await adminPage.GotoAsync(accountUrl);
            await adminPage.Locator("form[action='/Credit/Review'] input[name='reason']").FillAsync("Bank receipt verified");
            await adminPage.GetByRole(AriaRole.Button,new(){Name="ยืนยันรับโอนและคืนวงเงิน",Exact=true}).ClickAsync();
            Assert.Contains("ยืนยันยอดโอน ตัดหนี้ และคืนวงเงินแล้ว",await adminPage.Locator("main").InnerTextAsync());
            await adminPage.ScreenshotAsync(new(){Path=Path.Combine(screenshotDirectory,"credit-account-desktop.png"),FullPage=true});
            await adminPage.GetByRole(AriaRole.Link,new(){Name="ใบแจ้งยอด",Exact=true}).ClickAsync();
            await adminPage.Locator("[data-print]").ClickAsync();await adminPage.Locator("#report-modal").WaitForAsync(new(){State=WaitForSelectorState.Visible});
            Assert.Contains("รับโอนชำระลูกหนี้",await adminPage.Locator(".report-preview").InnerTextAsync());
            await adminPage.ScreenshotAsync(new(){Path=Path.Combine(screenshotDirectory,"credit-statement-popup.png"),FullPage=true});
            await page.GotoAsync(accountUrl);Assert.True(await page.EvaluateAsync<bool>("document.documentElement.scrollWidth <= window.innerWidth + 1"));
            await page.ScreenshotAsync(new(){Path=Path.Combine(screenshotDirectory,"credit-account-mobile.png"),FullPage=true});
            await guestContext.CloseAsync();Assert.Empty(jsErrors);
            await context.CloseAsync(); await otherContext.CloseAsync(); await adminContext.CloseAsync();
        }
        finally
        {
            if (!process.HasExited) process.Kill(true); await process.WaitForExitAsync(); await output; await errors;
        }
    }
}






