using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AmHerb.Web.Data;
using AmHerb.Web.Domain;
using AmHerb.Web.Models;
using AmHerb.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

System.Globalization.CultureInfo.DefaultThreadCurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
var repo = new DirectoryInfo(Directory.GetCurrentDirectory());
while (repo != null && !File.Exists(Path.Combine(repo.FullName, "AmHerb.sln"))) repo = repo.Parent;
if (repo == null) throw new InvalidOperationException("Run this tool from the AM HERB workspace.");
if (args.Contains("--preview")) { await DemoBrowserPreview.Run(repo.FullName); return; }
var configuration = new ConfigurationBuilder().AddUserSecrets("2735d6bf-4910-4c9c-b691-eb44579d7084").AddEnvironmentVariables().Build();
var connection = configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Configure the SQL Server connection in User Secrets.");
var services = new ServiceCollection();
services.AddLogging(); services.AddDataProtection(); services.AddHttpContextAccessor();
services.AddSingleton<IWebHostEnvironment>(new DemoEnvironment(Path.Combine(repo.FullName, "src", "AmHerb.Web")));
var clock = new DemoClock(); services.AddSingleton<TimeProvider>(clock);
services.AddDbContext<AmHerbDbContext>(options => options.UseSqlServer(connection));
services.AddIdentityCore<AppUser>(options => { options.Password.RequiredLength = 12; options.User.RequireUniqueEmail = true; })
    .AddRoles<IdentityRole>().AddEntityFrameworkStores<AmHerbDbContext>().AddDefaultTokenProviders();
services.AddScoped<Actor>(); services.AddScoped<AuditService>(); services.AddScoped<INotificationService, NotificationService>();
services.AddScoped<MemberService>(); services.AddScoped<PricingService>(); services.AddScoped<RewardService>();
services.AddScoped<InventoryService>(); services.AddScoped<CommerceService>(); services.AddScoped<PosService>();
services.AddScoped<CreditService>(); services.AddScoped<StockTransferService>(); services.AddScoped<ConversationService>();
services.AddScoped<RedemptionConsentService>(); services.AddScoped<DemoDataMaintenance>(); services.AddScoped<SalesDashboardService>();
services.AddScoped<CsvMarketplaceImportProvider>();
await using var provider = services.BuildServiceProvider();
await using var scope = provider.CreateAsyncScope();
var sp = scope.ServiceProvider;
var db = sp.GetRequiredService<AmHerbDbContext>();
if (db.Database.GetDbConnection().Database != "AmHerb") throw new InvalidOperationException("This demo tool is restricted to the configured AmHerb database.");
var root = await db.Members.Include(x => x.User).SingleOrDefaultAsync(x => x.User.UserName == "admin@amherb.local")
    ?? throw new InvalidOperationException("The existing admin@amherb.local member is required.");
var users = sp.GetRequiredService<UserManager<AppUser>>();
if (!await users.IsInRoleAsync(root.User, "SuperAdmin")) throw new InvalidOperationException("The original account must be SuperAdmin.");
if (args.Contains("--repair-dates"))
{
    string Normalize(string value) => System.Text.RegularExpressions.Regex.Replace(value, @"(25\d{2})-(\d{2})-(\d{2})", match => (int.Parse(match.Groups[1].Value) - 543) + "-" + match.Groups[2].Value + "-" + match.Groups[3].Value);
    foreach (var entry in await db.SystemSettings.Where(x => x.Key.StartsWith("Demo.Scenario.")).ToListAsync()) entry.Value = Normalize(entry.Value);
    await db.SaveChangesAsync();
    foreach (var file in new[] { "GUIDE.md", "summary.json" })
    {
        var path = Path.Combine(repo.FullName, "artifacts", "demo", file);
        await File.WriteAllTextAsync(path, Normalize(await File.ReadAllTextAsync(path)));
    }
    Console.WriteLine("Demo date links use Gregorian ISO dates."); return;
}
var maintenance = sp.GetRequiredService<DemoDataMaintenance>();
if (args.Contains("--snapshot-baseline"))
{ await maintenance.CompleteBaselineSnapshotAsync(); Console.WriteLine("Baseline SQL snapshots ready; reset also restores edits to original catalog and media."); return; }
if (args.Contains("--reset"))
{
    await maintenance.ResetAsync(root.UserId); Console.WriteLine("Demo reset complete; original admin, catalog, media and settings preserved."); return;
}
if (!args.Contains("--seed")) throw new InvalidOperationException("Specify --seed or --reset.");
var orderCount = 180;
var position = Array.IndexOf(args, "--orders");
if (position >= 0) orderCount = int.Parse(args[position + 1]);
if (orderCount is < 120 or > 500) throw new InvalidOperationException("Use 120 to 500 historical orders.");
if (await db.Members.CountAsync() != 1 || await db.Users.CountAsync() != 1) throw new InvalidOperationException("Baseline must contain only the original administrator. Reset an existing demo before reseeding.");
await maintenance.CreateBaselineAsync();
var password = Environment.GetEnvironmentVariable("AMHERB_DEMO_PASSWORD") ?? "Demo!" + Convert.ToHexString(RandomNumberGenerator.GetBytes(6)) + "a9";
var runner = new DemoRunner(sp, clock, root, repo.FullName, password);
await runner.Run(orderCount);

public sealed class DemoClock : TimeProvider
{
    public DateTime Now { get; set; } = DateTime.UtcNow;
    public override DateTimeOffset GetUtcNow() => new(DateTime.SpecifyKind(Now, DateTimeKind.Utc));
}
public sealed class DemoEnvironment(string contentRoot) : IWebHostEnvironment
{
    public string EnvironmentName { get; set; } = "Development";
    public string ApplicationName { get; set; } = "AmHerb.Web";
    public string ContentRootPath { get; set; } = contentRoot;
    public string WebRootPath { get; set; } = Path.Combine(contentRoot, "wwwroot");
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
}
public sealed class DemoRunner(IServiceProvider sp, DemoClock clock, Member root, string repo, string password)
{
    private readonly AmHerbDbContext db = sp.GetRequiredService<AmHerbDbContext>();
    private readonly UserManager<AppUser> users = sp.GetRequiredService<UserManager<AppUser>>();
    private readonly MemberService members = sp.GetRequiredService<MemberService>();
    private readonly CommerceService commerce = sp.GetRequiredService<CommerceService>();
    private readonly InventoryService inventory = sp.GetRequiredService<InventoryService>();
    private readonly PosService pos = sp.GetRequiredService<PosService>();
    private readonly RewardService rewards = sp.GetRequiredService<RewardService>();
    private readonly CreditService credits = sp.GetRequiredService<CreditService>();
    private readonly StockTransferService transfers = sp.GetRequiredService<StockTransferService>();
    private readonly ConversationService conversations = sp.GetRequiredService<ConversationService>();
    private readonly DateTime now = DateTime.UtcNow;
    private readonly List<Member> people = [root];
    private readonly List<DemoScenario> scenarios = [];
    private readonly List<(string Email, string Name, string Role)> accounts = [];
    private readonly Random random = new(4062026);
    private ClaimsPrincipal Principal(Member member, string role = "Member") => new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, member.UserId), new Claim(ClaimTypes.Name, member.User.UserName!), new Claim(ClaimTypes.Role, role)], "Demo"));
    private void Act(Member member, string role = "Member") => sp.GetRequiredService<IHttpContextAccessor>().HttpContext = new DefaultHttpContext { User = Principal(member, role) };
    private void Scenario(string title, string description, string url, string account, string expected) => scenarios.Add(new(title, description, url, account, expected));
    private CheckoutInput Input(Member buyer, string key, string? coupon = null) => new() { Key = "demo:" + key, CustomerName = buyer.Name, Email = buyer.User.Email, Phone = buyer.Phone, HouseNumber = "99/1 (ข้อมูลจำลอง)", SubdistrictCode = "100101", PostalCode = "10200", ShippingProviderId = 1, Address = buyer.Address, Coupon = coupon };
    private async Task Finish(Order order, bool verify = true)
    {
        Act(root, "SuperAdmin");
        if (order.Status == OrderStatus.PendingPayment && order.SlipRequired) await commerce.SubmitPaymentAsync(order.Id, "DEMO-NOT-A-REAL-TRANSFER-" + order.Id, Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aX1sAAAAASUVORK5CYII="));
        if (order.Status == OrderStatus.PendingPayment) await commerce.ConfirmPaymentAsync(order.Id, "DEMO-BANK-" + order.Id, order.CashPayable, "ข้อมูลจำลอง: ยืนยันเงินโอน ไม่มีการโอนเงินจริง");
        if (order.Status is OrderStatus.Paid or OrderStatus.Processing) await commerce.FulfillAsync(order.Id, "ขนส่งจำลอง", "DEMO-TRACK-" + order.Id, false, "ข้อมูลจำลอง: จัดส่ง");
        if (order.Status == OrderStatus.Shipped) await commerce.FulfillAsync(order.Id, "ขนส่งจำลอง", "DEMO-TRACK-" + order.Id, true, "ข้อมูลจำลอง: ส่งมอบ");
        if (verify && !order.StockLoading && order.Status == OrderStatus.Delivered) await commerce.VerifyRetailAsync(order.Id, "ข้อมูลจำลอง: หลักฐานขายปลีกและส่งมอบครบถ้วน");
    }
    public async Task Run(int orderCount)
    {
        Console.WriteLine("Preparing 40 members on SQL Server AmHerb; original admin credentials remain unchanged.");
        clock.Now = now.AddDays(-430); Act(root, "SuperAdmin");
        string[] firstNames = ["กานต์", "มาลี", "ธนา", "พิมพ์", "ณัฐ", "อร", "ภัทร", "ศิริ", "วริน", "สุธี", "ปรียา", "ธวัช", "ริน", "ชล", "นภา", "วิทย์", "นิต", "กฤต", "มน", "จิรา"];
        var staffRoles = new[] { "Finance", "Marketing", "Warehouse", "CustomerService", "Admin" };
        for (var i = 1; i < 40; i++)
        {
            var email = $"demo{i:D2}@amherb.example";
            var user = new AppUser { Email = email, UserName = email, EmailConfirmed = true };
            var result = await users.CreateAsync(user, password);
            if (!result.Succeeded) throw new InvalidOperationException(string.Join("; ", result.Errors.Select(x => x.Description)));
            await users.AddToRoleAsync(user, "Member");
            var role = i <= 5 ? staffRoles[i - 1] : "Member";
            if (i <= 5) await users.AddToRoleAsync(user, role);
            var sponsor = people[(i - 1) / 3];
            var member = await members.CreateAsync(user.Id, firstNames[(i - 1) % firstNames.Length] + $" ตัวอย่าง{i:D2}", sponsor.ReferralCode);
            member.User = user; member.Code = $"AM-DEMO-{i:D3}"; member.ReferralCode = $"demo{i:D3}";
            member.Phone = $"000000{i:D4}"; member.Address = $"บ้านตัวอย่าง {i} ถนนจำลอง เขตทดสอบ จังหวัดกรุงเทพมหานคร 00000";
            member.CreatedAt = i <= 15 ? now.AddDays(-430 + i) : now.AddDays(-120 + (i - 16) * 4); member.Type = i is >= 6 and <= 15 ? MemberType.Reseller : MemberType.Member;
            var warehouse = await db.Warehouses.SingleAsync(x => x.OwnerMemberId == member.Id); warehouse.Name = "คลังจำลอง " + member.Code;
            if (i is >= 6 and <= 15) warehouse.Kind = WarehouseKind.Dealer;
            var register = await db.PosRegisters.SingleAsync(x => x.UserId == user.Id); register.Name = "POS จำลอง " + member.Code;
            await db.SaveChangesAsync(); people.Add(member); accounts.Add((email, member.Name, role));
        }
        if (await db.Members.CountAsync() != 40) throw new InvalidOperationException("Member total must be exactly 40.");
        var additions = new[] { ("DEMO-HERBAL-TEA", "ชาสมุนไพรตัวอย่าง", 199m), ("DEMO-HONEY", "น้ำผึ้งตัวอย่าง", 349m), ("DEMO-GIFT", "ชุดของขวัญตัวอย่าง", 890m) };
        foreach (var (code, name, price) in additions)
        {
            var sku = new Sku { Code = code, Barcode = code, Cost = Money.Round(price * .42m), LowStockThreshold = 10, Product = new Product { Name = name, Category = "สินค้าสาธิต", Description = "ข้อมูลสินค้าและราคาจำลองสำหรับทดลองระบบ ไม่ใช่ฉลากสินค้าที่รับรอง" } };
            db.Skus.Add(sku); await db.SaveChangesAsync();
            foreach (var tier in new[] { ("Retail", 1, price), ("Wholesale", 1, Money.Round(price * .7m)), ("Pack6", 6, Money.Round(price * .8m) * 6) })
                db.ProductPrices.Add(new ProductPrice { SkuId = sku.Id, Tier = tier.Item1, PackQuantity = tier.Item2, Amount = tier.Item3, EffectiveFrom = clock.Now.AddDays(-1) });
            db.TokenRates.Add(new TokenRate { SkuId = sku.Id, BaseToken = 15, EffectiveFrom = clock.Now.AddDays(-1) }); await db.SaveChangesAsync();
        }
        var skus = await db.Skus.Include(x => x.Product).OrderBy(x => x.Id).ToListAsync();
        var retailPrices = await db.ProductPrices.Where(x => x.Tier == "Retail" && x.Channel == null).ToDictionaryAsync(x => x.SkuId, x => x.Amount);
        foreach (var sku in skus)
        {
            var cost = Money.Round(retailPrices[sku.Id] * .42m);
            await inventory.ReceiveAsync(sku.Id, 1, "DEMO-LONG-" + sku.Code, clock.Now.AddDays(-30), now.AddDays(400), 1500, cost, "ข้อมูลจำลอง: รับสินค้าคลังกลาง");
            await inventory.ReceiveAsync(sku.Id, 1, "DEMO-SOON-" + sku.Code, clock.Now.AddDays(-30), now.AddDays(45), 200, cost, "ข้อมูลจำลอง: ล็อตใกล้หมดอายุ");
        }
        for (var i = 6; i <= 15; i++)
        {
            var member = people[i]; var destination = await db.Warehouses.SingleAsync(x => x.OwnerMemberId == member.Id);
            foreach (var sku in skus)
            {
                Act(member); var transfer = await transfers.RequestAsync(Principal(member), Guid.NewGuid(), 1, destination.Id, sku.Id, 80, "ข้อมูลจำลอง: เติมสินค้าจุดขาย");
                Act(root, "SuperAdmin"); await transfers.DispatchAsync(Principal(root, "SuperAdmin"), transfer.Id, "DEMO-SHIP-" + transfer.Id, "ข้อมูลจำลอง: ผู้ดูแลอนุมัติการเบิก");
                Act(member); await transfers.ReceiveAsync(Principal(member), transfer.Id, "ข้อมูลจำลอง: ผู้ขายตรวจและรับสินค้าครบ");
            }
        }
        db.Promotions.AddRange(
            new Promotion { Code = "DEMO10", Kind = PromotionKind.Percentage, Value = 10, UsageLimit = 10000, EffectiveFrom = now.AddDays(-450), EffectiveTo = now.AddDays(90), Reason = "ส่วนลดจำลอง 10%" },
            new Promotion { Code = "DEMO50", Kind = PromotionKind.Fixed, Value = 50, UsageLimit = 10000, EffectiveFrom = now.AddDays(-450), EffectiveTo = now.AddDays(90), Reason = "ส่วนลดจำลอง 50 บาท" },
            new Promotion { Code = "DEMO-GIFT", Kind = PromotionKind.FreeGift, GiftSkuId = skus.First(x => x.Code == "DEMO-HERBAL-TEA").Id, MinimumSpend = 990, UsageLimit = 10000, EffectiveFrom = now.AddDays(-450), EffectiveTo = now.AddDays(90), Reason = "ของแถมจำลอง" });
        await db.SaveChangesAsync();
        clock.Now = now.AddDays(-400); Act(people[6]);
        var oldOrder = await commerce.CheckoutAsync(new CheckoutInput { Key = "demo:expired-token", CustomerName = "ลูกค้าตัวอย่างเก่า", Email = "oldguest@amherb.example", Phone = "0000008888", HouseNumber = "99/1 (ข้อมูลจำลอง)", SubdistrictCode = "100101", PostalCode = "10200", ShippingProviderId = 1, Address = "ที่อยู่จำลองสำหรับยอดขายเก่า" }, [new(skus[0].Id, 1)], null, people[6].Id);
        await Finish(oldOrder);
        Scenario("Token หมดอายุ", "ยอดขายเก่ากว่า 12 เดือนถูกปลดล็อกและหมดอายุตามนโยบาย", "/Admin?section=Ledger", "admin@amherb.local", "พบ RELEASE และ EXPIRY ใน Ledger");
        Order? refundExample = null; Order? couponExample = null; Order? selfExample = null;
        var sessions = new Dictionary<int, long>();
        for (var n = 0; n < orderCount; n++)
        {
            clock.Now = now.AddDays(-89 + 89d * n / orderCount);
            var sellerIndex = 6 + n % 10; var seller = people[sellerIndex];
            var eligibleBuyers = people.Skip(16).Where(x => x.CreatedAt <= clock.Now).ToList();
            var buyer = eligibleBuyers[n % eligibleBuyers.Count];
            var self = n % 41 == 1; if (self) buyer = seller;
            var channel = (SalesChannel)(n % 6);
            var sku = skus[n % 5 == 0 ? 6 : random.Next(skus.Count)];
            var quantity = random.Next(1, 4);
            var wholesale = n % 37 == 2 && sku.Code != "DETOXIFY-BLUE";
            if (wholesale) { channel = SalesChannel.Store; sku = skus[0]; }
            var coupon = n % 17 == 3 ? "DEMO50" : n % 11 == 4 ? "DEMO10" : null;
            var pending = n % 23 == 5 || n % 29 == 6;
            var input = Input(buyer, "sale-" + n, coupon);
            var lines = new List<SaleLine> { new(sku.Id, quantity, wholesale ? "Wholesale" : "Retail") };
            if (n % 9 == 0 && !wholesale) lines.Add(new(skus[1].Id, 1));
            long? session = null;
            if (channel == SalesChannel.POS)
            {
                if (!sessions.TryGetValue(sellerIndex, out var sessionId)) { Act(seller); await pos.OpenAsync(seller.UserId, 1000); sessionId = await db.PosSessions.Where(x => x.PosRegister.UserId == seller.UserId && x.ClosedAt == null).Select(x => x.Id).SingleAsync(); sessions[sellerIndex] = sessionId; }
                session = sessionId;
            }
            Act(seller);
            var order = await commerce.CheckoutAsync(input, lines, buyer.Id, seller.Id, channel,
                channel == SalesChannel.POS ? seller.UserId : null, session, 100000,
                pending || n % 4 == 1 ? "ManualBankTransfer" : "Cash",
                channel is SalesChannel.Shopee or SalesChannel.TikTokShop or SalesChannel.Lazada ? "DEMO-EXT-" + n : null,
                channel is SalesChannel.Shopee or SalesChannel.TikTokShop or SalesChannel.Lazada ? Money.Round(retailPrices[sku.Id] * quantity * .05m) : 0,
                channel == SalesChannel.TikTokShop ? Money.Round(retailPrices[sku.Id] * quantity * .03m) : 0,
                new[] { "ดูแลสุขภาพรายวัน", "รีวิวสินค้า Q4", "สมาชิกแนะนำเพื่อน", "โปรโมชันต้นเดือน" }[n % 4], "ครีเอเตอร์จำลอง " + (n % 8 + 1));
            if (pending)
            {
                if (order.Status != OrderStatus.PendingPayment) throw new InvalidOperationException("Pending example unexpectedly settled.");
                if (n % 29 == 6) { Act(root, "SuperAdmin"); await commerce.CancelAsync(order.Id, "ข้อมูลจำลอง: ลูกค้ายกเลิกก่อนชำระ"); }
            }
            else
            {
                await Finish(order);
                if (self) selfExample = order;
                if (coupon != null) couponExample = order;
                if (n % 19 == 8)
                {
                    await commerce.RequestReturnAsync(order.Items[0].Id, 1, "ข้อมูลจำลอง: ลูกค้าขอคืนสินค้า");
                    var request = await db.ReturnRequests.SingleAsync(x => x.OrderItemId == order.Items[0].Id);
                    await commerce.ApproveReturnAsync(request.Id, n % 2 == 0, "DEMO-REFUND-" + n, "ข้อมูลจำลอง: ตรวจและอนุมัติคืนเงิน"); refundExample = order;
                }
            }
            db.ReferralVisits.Add(new ReferralVisit { MemberId = seller.Id, Campaign = order.Campaign, Source = channel.ToString(), CreatedAt = clock.Now.AddMinutes(-30), ConvertedOrderId = pending ? null : order.Id });
            if (n % 3 == 0) for (var v = 0; v < 3; v++) db.ReferralVisits.Add(new ReferralVisit { MemberId = seller.Id, Campaign = order.Campaign, Source = "Social", CreatedAt = clock.Now.AddMinutes(-45-v) });
            await db.SaveChangesAsync();
            if ((n + 1) % 30 == 0) Console.WriteLine($"Created {n + 1}/{orderCount} historical orders using commerce services.");
        }
        clock.Now = now; Act(root, "SuperAdmin"); await rewards.RunDueAsync();
        foreach (var (index, session) in sessions)
        {
            var opening = await db.PosSessions.Where(x => x.Id == session).Select(x => x.OpeningCash).SingleAsync();
            var cash = await db.Payments.Where(x => x.Order.PosSessionId == session && x.Provider == "Cash" && x.Status == "Confirmed").SumAsync(x => x.Amount);
            Act(people[index]); await pos.CloseAsync(people[index].UserId, session, opening + cash, "ข้อมูลจำลอง: ตรวจเงินสดและปิดกะตรงยอด");
        }
        await ExtraExamples(skus);
        await ReportsAndGuide(skus, couponExample, refundExample, selfExample);
    }
    private async Task ExtraExamples(List<Sku> skus)
    {
        clock.Now = now; Act(root, "SuperAdmin");
        var cashier = people[6]; var buyer = people[7];
        await pos.OpenAsync(cashier.UserId, 1000);
        var session = await db.PosSessions.Include(x => x.PosRegister).SingleAsync(x => x.PosRegister.UserId == cashier.UserId && x.ClosedAt == null);
        var wallet = await rewards.BalanceAsync(buyer.Id);
        if (wallet.Available < 10) throw new InvalidOperationException("Token demo requires a released wallet balance.");
        var tokens = Math.Min(100, wallet.Available);
        var consent = sp.GetRequiredService<RedemptionConsentService>();
        var approval = await consent.IssueAsync(buyer.Id, session.PosRegisterId, tokens);
        var tokenInput = Input(buyer, "token-redemption"); tokenInput.Tokens = tokens; tokenInput.RedemptionCode = approval;
        Act(cashier); var tokenOrder = await commerce.CheckoutAsync(tokenInput, [new(skus[0].Id, 1)], buyer.Id, cashier.Id, SalesChannel.POS, cashier.UserId, session.Id, 1000);
        await Finish(tokenOrder);
        Scenario("แลก Token ที่ POS", "ลูกค้าสร้างรหัสอนุมัติ แล้วใช้ Token ลดเงินสดที่ต้องชำระ", "/Orders/Detail/" + tokenOrder.PublicId, cashier.User.UserName!, $"ใช้ Token {tokens:N2} / เงินชำระ {tokenOrder.CashPayable:N2} บาท / รหัสใช้ได้ครั้งเดียว");
        Act(cashier); var pendingOrder = await commerce.CheckoutAsync(Input(people[20], "pending-transfer"), [new(skus[6].Id, 2)], people[20].Id, cashier.Id, SalesChannel.POS, cashier.UserId, session.Id, 0, "ManualBankTransfer");
        Scenario("POS โอนเงินรอตรวจ", "ขายโดยแจ้งโอน ฝ่ายการเงินยังไม่ได้ยืนยัน", "/Orders/Detail/" + pendingOrder.PublicId, "demo01@amherb.example", "สถานะ PendingPayment และสต็อกถูกจอง ยังไม่มีรางวัล");
        var gift = await commerce.CheckoutAsync(Input(people[22], "free-gift", "DEMO-GIFT"), [new(skus[1].Id, 2)], people[22].Id, cashier.Id);
        await Finish(gift);
        Scenario("โปรโมชันของแถม", "ซื้อสินค้าตามเงื่อนไขและรับชาสมุนไพรตัวอย่าง", "/Orders/Detail/" + gift.PublicId, "admin@amherb.local", "มีสินค้าของแถมราคา 0 และตัดสต็อกของแถมด้วย");
        for (var i = 0; i < 3; i++)
        {
            var customer = people[6 + i]; clock.Now = now.AddDays(i == 2 ? -40 : -12 + i * 6); Act(root, "SuperAdmin");
            await credits.Configure(customer.Id, 25000, 30, true, true, "ข้อมูลจำลอง: วงเงินเครดิตตัวแทน", null);
            var input = Input(customer, "credit-" + i); input.UseCredit = true;
            var order = await commerce.CheckoutAsync(input, [new(skus[0].Id, 4, "Wholesale")], customer.Id, people[15].Id);
            await Finish(order, false);
            var account = await db.CreditAccounts.SingleAsync(x => x.MemberId == customer.Id);
            clock.Now = now; Act(customer);
            var receipt = await credits.Submit(account.Id, customer.UserId, false, Guid.NewGuid(), i == 1 ? order.CashPayable : 500, now.AddHours(-1), "DEMO-CREDIT-" + i, "สลิปและยอดเงินจำลอง", null);
            Act(root, "SuperAdmin"); await credits.Review(receipt, i != 2, root.UserId, i == 2 ? "ข้อมูลจำลอง: ปฏิเสธสลิปที่ยอดไม่ตรง" : "ข้อมูลจำลอง: ตรวจรับโอนและคืนวงเงิน");
            if (i == 0)
            {
                Act(customer); await credits.Submit(account.Id, customer.UserId, false, Guid.NewGuid(), 100, now.AddMinutes(-30), "DEMO-CREDIT-PENDING", "ข้อมูลจำลอง: รอฝ่ายการเงินตรวจ", null);
                Act(root, "SuperAdmin"); await commerce.RequestReturnAsync(order.Items[0].Id, 1, "ข้อมูลจำลอง: คืนสินค้าซื้อเชื่อ");
                var request = await db.ReturnRequests.SingleAsync(x => x.OrderItemId == order.Items[0].Id);
                await commerce.ApproveReturnAsync(request.Id, true, "DEMO-CREDIT-RETURN", "ข้อมูลจำลอง: ลดหนี้ก่อนคืนเงิน");
            }
            Scenario(i == 0 ? "เครดิตชำระบางส่วนและคืนสินค้า" : i == 1 ? "เครดิตชำระครบคืนวงเงิน" : "เครดิตค้างเกินกำหนด", "วงเงิน 25,000 บาท ซื้อเชื่อ และตรวจรับโอนตามบทบาท", "/Credit/Account/" + account.Id, customer.User.UserName!, i == 0 ? "มีหนี้คงเหลือ รายการโอนรอตรวจ และการลดหนี้จากคืนสินค้า" : i == 1 ? "หนี้เป็นศูนย์ วงเงินกลับมาเต็ม" : "มีหนี้เกินกำหนดและสลิปถูกปฏิเสธ");
        }
        clock.Now = now; var warehouse = await db.Warehouses.SingleAsync(x => x.OwnerMemberId == cashier.Id);
        var request1 = await transfers.RequestAsync(Principal(cashier), Guid.NewGuid(), 1, warehouse.Id, skus[0].Id, 20, "ข้อมูลจำลอง: ใบเบิกรออนุมัติ");
        var request2 = await transfers.RequestAsync(Principal(cashier), Guid.NewGuid(), 1, warehouse.Id, skus[1].Id, 15, "ข้อมูลจำลอง: สินค้ากำลังเดินทาง");
        await transfers.DispatchAsync(Principal(root, "SuperAdmin"), request2.Id, "DEMO-IN-TRANSIT", "ข้อมูลจำลอง: จ่ายสินค้าแล้วรอรับ");
        var request3 = await transfers.RequestAsync(Principal(cashier), Guid.NewGuid(), 1, warehouse.Id, skus[2].Id, 10, "ข้อมูลจำลอง: ใบเบิกที่ไม่ใช้แล้ว");
        await transfers.CancelAsync(Principal(cashier), request3.Id, "ข้อมูลจำลอง: ยกเลิกก่อนจ่ายสินค้า");
        Scenario("เบิกสินค้ารออนุมัติ", "เจ้าของคลังส่งคำขอเบิกให้ผู้ดูแล", "/Transfers/Detail/" + request1.Id, cashier.User.UserName!, "ผู้ดูแลอนุมัติได้ แต่สมาชิกอื่นเข้ารายการนี้ไม่ได้");
        Scenario("สินค้าระหว่างขนส่ง", "คลังกลางจ่ายแล้ว แต่ปลายทางยังไม่รับ", "/Transfers/Detail/" + request2.Id, cashier.User.UserName!, "สถานะ Dispatched และแสดงยอด In transit");
        Act(root, "SuperAdmin");
        var batch = await db.InventoryBatches.Where(x => x.WarehouseId == warehouse.Id && x.SkuId == skus.Last().Id && x.QtyAvailable > 5).FirstAsync();
        await inventory.AdjustAsync(batch.Id, 5 - batch.QtyAvailable, InventoryKind.Adjust, "ข้อมูลจำลอง: ตัวอย่างเตือนสต็อกต่ำ");
        clock.Now = now.AddDays(-100);
        await inventory.ReceiveAsync(skus[2].Id, warehouse.Id, "DEMO-EXPIRED", clock.Now.AddDays(-30), now.AddDays(-2), 8, 200, "ข้อมูลจำลอง: ล็อตที่หมดอายุในวันนี้");
        clock.Now = now;
        var service = conversations;
        for (var i = 0; i < 6; i++)
        {
            var customer = people[6 + i]; Act(customer);
            var thread = await service.Send(Principal(customer), null, null, new[] { "สอบถามการส่งสินค้า", "ขอคำแนะนำรายการสินค้า", "ตรวจยอดเครดิต" }[i % 3], "ข้อความจำลอง: ขอให้ทีมงานตรวจสอบข้อมูลให้หน่อยครับ", Guid.NewGuid());
            if (i % 3 != 0)
            {
                Act(people[4], "CustomerService"); await service.Read(Principal(people[4], "CustomerService"), thread);
                await service.Send(Principal(people[4], "CustomerService"), thread, customer.Id, null, "คำตอบจำลอง: ตรวจสอบแล้ว กรุณาดูรายละเอียดในบัญชีของคุณ", Guid.NewGuid());
                if (i % 3 == 2) await service.Close(Principal(people[4], "CustomerService"), thread, "ข้อมูลจำลอง: ช่วยเหลือเสร็จแล้ว");
            }
        }
        db.ContentPosts.AddRange(
            new ContentPost { Title = "ตัวอย่าง: โปรโมชันสินค้าเดือนนี้", Body = "ข้อมูลสาธิต: ใช้คูปอง DEMO10 รับส่วนลด 10% ในระบบจำลอง ไม่มีการส่งเงินจริง", Published = true, StartsAt = now.AddDays(-30), EndsAt = now.AddDays(30), Kind = ContentKind.Advertisement, Audience = ContentAudience.Public },
            new ContentPost { Title = "ตัวอย่าง: ข่าวสำหรับสมาชิก", Body = "ข้อมูลสาธิต: ตรวจคลังส่วนตัว เปิด POS และดูยอด Token ของตนเอง", Published = true, StartsAt = now.AddDays(-10), EndsAt = now.AddDays(30), Kind = ContentKind.News, Audience = ContentAudience.Members },
            new ContentPost { Title = "ตัวอย่าง: ข่าวที่ยังเป็นร่าง", Body = "รายการนี้ยังไม่เผยแพร่ ใช้ทดลองจัดการเนื้อหา", Published = false, StartsAt = now, EndsAt = now.AddDays(30), Audience = ContentAudience.Everyone });
        db.Carts.Add(new Cart { UpdatedAt = now.AddDays(-40), Items = [new CartItem { SkuId = skus[0].Id, Quantity = 1 }] }); await db.SaveChangesAsync();
        var jobs = new RecurringJobs(db, rewards, commerce, sp.GetRequiredService<INotificationService>(), [], clock);
        Act(root, "SuperAdmin"); await jobs.RunAsync(CancellationToken.None);
        Scenario("งานอัตโนมัติและแจ้งเตือน", "ปลดล็อก Token ยกเลิกรายการรอชำระที่หมดเวลา ล้างตะกร้าเก่า และตรวจล็อต", "/Member", cashier.User.UserName!, "พบแจ้งเตือนสต็อกต่ำ ใกล้หมดอายุ และประวัติ Token");
        await ImportExamples(skus);
    }
    private async Task ImportExamples(List<Sku> skus)
    {
        var csv = new StringBuilder("Channel,ExternalOrderId,Sku,Quantity,CustomerName,Email,Phone,Address,ReferralCode,Campaign,Creator,ChannelFee,AffiliateFee,FulfillmentStatus,TransactionRef,PaidAmount,Carrier,TrackingNumber\r\n");
        var price = await sp.GetRequiredService<PricingService>().PriceAsync(skus[0].Id, SalesChannel.Shopee);
        foreach (var channel in new[] { SalesChannel.Shopee, SalesChannel.TikTokShop, SalesChannel.Lazada })
            csv.AppendLine($"{channel},DEMO-CSV-{channel},{skus[0].Code},1,ลูกค้าจำลอง Marketplace,market@amherb.example,0000009999,ที่อยู่จำลอง,{people[6].ReferralCode},CSV ตัวอย่าง,ครีเอเตอร์จำลอง,35,15,Delivered,DEMO-CSV-BANK-{channel},{price.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture)},ขนส่งจำลอง,DEMO-CSV-TRACK-{channel}");
        var directory = Path.Combine(repo, "artifacts", "demo"); Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(Path.Combine(directory, "marketplace-example.csv"), csv.ToString());
        var importer = sp.GetRequiredService<CsvMarketplaceImportProvider>();
        async Task Import()
        {
            await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv.ToString())); var result = await importer.ImportAsync(stream, root.UserId);
            if (result.Any(x => !x.Contains(": OK "))) throw new InvalidOperationException(string.Join("; ", result));
        }
        await Import(); var count = await db.Orders.CountAsync(); await Import();
        if (count != await db.Orders.CountAsync()) throw new InvalidOperationException("CSV replay duplicated orders.");
        Scenario("Marketplace CSV", "นำเข้า Shopee TikTok Shop และ Lazada แล้วนำเข้าซ้ำ", "/Admin?section=Imports", "admin@amherb.local", "เพิ่ม 3 คำสั่งซื้อครั้งเดียว ไม่มีรายการซ้ำ ใช้ไฟล์ marketplace-example.csv ในคู่มือ");
    }
    private async Task ReportsAndGuide(List<Sku> skus, Order? coupon, Order? refund, Order? self)
    {
        clock.Now = now; Act(root, "SuperAdmin");
        Scenario("ภาพรวมยอดขายและการตลาด", "ยอดขายย้อนหลัง 90 วัน ครบ 6 ช่องทางและ 4 แคมเปญ", "/Dashboard/Overview?from=" + now.AddDays(-89).AddHours(7).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) + "&to=" + now.AddHours(7).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture), "admin@amherb.local", "เห็นแนวโน้ม สินค้าขายดี ผลแคมเปญ และกำไรหลังต้นทุน");
        Scenario("สินค้าคงคลังของตัวเอง", "สมาชิกมีคลังและ POS แยกเป็นรายคน", "/Dashboard/Mine", "demo06@amherb.example", "เห็นเฉพาะยอดขายและสินค้าของบัญชีนี้");
        Scenario("ต้นทุน ล็อต และ FEFO", "ใช้ต้นทุนต่อชิ้นประมาณ 42% ของราคาปลีก และเลือกลอตหมดอายุก่อน", "/Stock", "demo03@amherb.example", "พบล็อตปกติ ใกล้หมดอายุ หมดอายุ และสต็อกต่ำ");
        Scenario("เครือข่ายสมาชิก 40 คน", "เครือข่าย 3 สายต่อชั้น มีรางวัล Seller และ L1–L3", "/Admin?section=Tree", "admin@amherb.local", "ครบ 40 สมาชิกและไม่มีวงจรสายงาน");
        Scenario("บัญชีเดบิต–เครดิต", "รายการขาย ต้นทุน รับชำระเครดิต และคืนสินค้าเกิด Journal ผ่านบริการจริง", "/Accounting", "demo01@amherb.example", "เดบิตและเครดิตของทุกสมุดรายวันเท่ากัน");
        Scenario("การเงินและหลักฐานโอน", "เงินสดรับจริงตามสูตรของระบบ เงินโอนและยอดเครดิตเป็นค่าจำลอง", "/Admin?section=Payments", "demo01@amherb.example", "แยก Confirmed, Pending, CreditApproved และ Cancelled");
        Scenario("ข่าวและโปรโมชันตามกลุ่ม", "มีข่าวสาธารณะ ข่าวสมาชิก และรายการร่าง", "/News/Manage", "demo02@amherb.example", "รายการร่างไม่แสดงสาธารณะ ข่าวสมาชิกแสดงหลังเข้าสู่ระบบ");
        Scenario("ข้อความและงานบริการลูกค้า", "มีเรื่องรอตอบ รอลูกค้า และเรื่องที่ปิดแล้ว", "/Messages", "demo04@amherb.example", "ทีมบริการเห็นงานของสมาชิก ผู้ใช้เห็นเฉพาะเรื่องของตน");
        Scenario("Audit trail", "ทุกขั้นตอนทางการเงิน สต็อก และการจัดส่งบันทึกประวัติ", "/Admin?section=AuditLogs", "admin@amherb.local", "มีหลักฐานการทำรายการโดยผู้ขายและผู้อนุมัติ");
        if (coupon != null) Scenario("ส่วนลดและยอดเงินสุทธิ", "คูปอง DEMO10 และ DEMO50 คำนวณส่วนลดและกระจายไปยังสินค้า", "/Orders/Detail/" + coupon.PublicId, "admin@amherb.local", "ยอดรวม ส่วนลด ค่าจัดส่ง และยอดเงินที่ต้องรับตรงกัน");
        if (refund != null) Scenario("คืนสินค้าและย้อนรางวัล", "คืนบางส่วนหรือคืนทั้งชิ้น พร้อมคืนเงินและย้อน Token ตามผู้รับเดิม", "/Orders/Detail/" + refund.PublicId, "admin@amherb.local", "สินค้าคืนขายได้กลับเข้าสต็อก สินค้าเสียหายไม่กลับเข้าสต็อก");
        if (self != null && await db.TokenDistributions.AnyAsync(x => x.SourceOrderId == self.Id)) throw new InvalidOperationException("Self purchase must not generate network rewards.");
        var journal = await db.JournalEntries.Include(x => x.Lines).AsNoTracking().ToListAsync(); foreach (var entry in journal) AccountingService.Validate(entry);
        if (await db.InventoryBatches.AnyAsync(x => x.QtyAvailable < 0 || x.QtyReserved < 0)) throw new InvalidOperationException("Negative stock.");
        if (await db.CreditInvoices.AnyAsync(x => x.Amount < x.Paid + x.Adjusted)) throw new InvalidOperationException("Invalid credit balance.");
        if (await db.MemberClosures.AnyAsync(x => x.AncestorMemberId == x.DescendantMemberId && x.Depth != 0)) throw new InvalidOperationException("Hierarchy cycle.");
        if (await db.Members.CountAsync() != 40) throw new InvalidOperationException("Invalid final member count.");
        foreach (var (scenario, index) in scenarios.Select((value, i) => (value, i)))
        {
            var value = JsonSerializer.Serialize(scenario, new JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
            if (value.Length > 1000) throw new InvalidOperationException("Scenario exceeds settings limit.");
            db.SystemSettings.Add(new SystemSetting { Key = $"Demo.Scenario.{index:D2}", Value = value });
        }
        (await db.SystemSettings.SingleAsync(x => x.Key == "Demo.State")).Value = "Ready";
        await db.SaveChangesAsync();
        var totals = await sp.GetRequiredService<SalesDashboardService>().BuildAsync(null, now.AddDays(-89).AddHours(7).Date, now.AddHours(7).Date, null);
        var summary = new { Database = "AmHerb", Members = await db.Members.CountAsync(), Products = skus.Count, Orders = await db.Orders.CountAsync(), Batches = await db.InventoryBatches.CountAsync(), Transfers = await db.StockTransfers.CountAsync(), JournalEntries = journal.Count, NetSales = totals.Current.Net, Cost = totals.Current.Cost, Contribution = totals.Current.Contribution, Tokens = await db.TokenLedger.CountAsync(), Scenarios = scenarios, Checks = new[] { "40 members", "balanced journals", "nonnegative stock", "valid credit balances", "self purchase has no rewards", "CSV replay is idempotent" } };
        var directory = Path.Combine(repo, "artifacts", "demo"); Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(Path.Combine(directory, "summary.json"), JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true }));
        var guide = new StringBuilder("# AM HERB — คู่มือข้อมูลจำลอง\n\nฐานข้อมูล: AmHerb บน SQL Server จริง\n\nเปิด http://localhost:5087/Demo ด้วยแอดมินเดิม\n\nบัญชี admin@amherb.local ใช้รหัสผ่านเดิม ไม่ถูกเปลี่ยนโดยเครื่องมือนี้\n\nรหัสผ่านบัญชี demo01 ถึง demo39: `" + password + "`\n\n| Username | ชื่อ | บทบาท |\n|---|---|---|\n");
        foreach (var account in accounts) guide.AppendLine($"| {account.Email} | {account.Name} | {account.Role} |");
        guide.AppendLine("\n## ตัวอย่างแต่ละระบบ\n"); foreach (var scenario in scenarios) guide.AppendLine($"- **{scenario.Title}** — {scenario.Description}\n  เปิด http://localhost:5087{scenario.Url}\n  บัญชี {scenario.Account}; ตรวจสอบ: {scenario.Expected}\n");
        guide.AppendLine("\n## รีเซ็ต\n\nเปิด /Demo แล้วพิมพ์ RESET DEMO หรือหยุดเว็บแล้วรัน scripts/Reset-Demo.ps1 ข้อมูลที่เพิ่มหลังเริ่มชุดทดสอบจะถูกล้าง และเก็บข้อมูลเดิมก่อนทดสอบไว้\n\nPromptPay, Card, LINE และ marketplace API ยังไม่มี credentials การทดสอบนี้ใช้โอนจำลองและ CSV ไม่ได้ส่งเงินจริงหรือเรียกบริการเหล่านั้น\n");
        await File.WriteAllTextAsync(Path.Combine(directory, "GUIDE.md"), guide.ToString());
        Console.WriteLine($"Demo ready: 40 members, {skus.Count} products, {summary.Orders} orders, {summary.Batches} batches, {journal.Count} balanced journals.");
        Console.WriteLine($"90-day net sales: {totals.Current.Net:N2}; contribution: {totals.Current.Contribution:N2} THB.");
        Console.WriteLine("Demo credentials and examples saved in artifacts/demo/GUIDE.md (ignored by Git).");
    }
}
