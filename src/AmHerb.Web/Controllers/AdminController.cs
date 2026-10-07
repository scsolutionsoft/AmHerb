using System.Data;
using System.Globalization;
using System.Text;
using AmHerb.Web.Data;
using AmHerb.Web.Domain;
using AmHerb.Web.Models;
using AmHerb.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AmHerb.Web.Controllers;
[Authorize(Policy = "BackOffice")]
public class AdminController(AmHerbDbContext db, IAuthorizationService authorization, CommerceService commerce, InventoryService inventory,
    MemberService members, PricingService pricing, AuditService audit, Actor actor, UserManager<AppUser> users, CsvMarketplaceImportProvider importer) : Controller
{
    private static string Permission(string section) => section switch { "Products" or "Prices" or "Promotions" => "Prices", "TokenRates" or "TokenPolicies" or "Ledger" => "Tokens", "Inventory" or "StockCard" or "Warehouses" => "Inventory", "Members" or "Tree" => "Members", "Orders" => "Orders", "Payments" or "Imports" => "Payments", "Returns" => "Refunds", "Reports" or "Dashboard" => "Reports", _ => "Settings" };
    public async Task<IActionResult> Index(string section = "Dashboard", string? q = null, int page = 1, DateTime? from = null, DateTime? to = null)
    {
        if (section == "Dashboard" && !(await authorization.AuthorizeAsync(User, "Reports")).Succeeded)
            foreach (var candidate in new[] { "Orders", "Inventory", "Members", "Products" })
                if ((await authorization.AuthorizeAsync(User, Permission(candidate))).Succeeded) { section = candidate; break; }
        if (!(await authorization.AuthorizeAsync(User, Permission(section))).Succeeded) return Forbid();
        if (section == "Dashboard") return RedirectToAction("Overview", "Dashboard", new { from, to });
        var model = await PageAsync(section, q, Math.Max(1, page), from, to); return View(model);
    }
    private async Task<AdminPage> PageAsync(string section, string? q, int page, DateTime? from, DateTime? to)
    {
        var m = new AdminPage { Section = section, Title = section, Page = page, Search = q }; var skip = (page - 1) * 100;
        string N(decimal value) => value.ToString("N2");
        if (section is "Dashboard" or "Reports")
        {
            var start = from?.ToUniversalTime() ?? DateTime.UtcNow.AddDays(-30); var end = to?.ToUniversalTime().AddDays(1) ?? DateTime.UtcNow.AddDays(1);
            var orders = await db.Orders.Where(x => x.CreatedAt >= start && x.CreatedAt < end && x.Status != OrderStatus.Cancelled && x.Status != OrderStatus.PendingPayment).Include(x => x.Items).ThenInclude(x => x.Allocations).ThenInclude(x => x.InventoryBatch).ToListAsync();
            var ids = orders.Select(x => x.Id).ToArray(); var refunds = await db.ReturnRequests.Where(x => x.Status == "Approved" && ids.Contains(x.OrderItem.OrderId)).SumAsync(x => (decimal?)x.CashRefund) ?? 0;
            var gross = orders.Sum(x => x.Merchandise);
            var net = orders.SelectMany(x => x.Items).Sum(x => (x.UnitPrice * x.Quantity - x.Discount) * (x.Quantity - x.ReturnedQuantity) / x.Quantity);
            var returnedCost = await db.InventoryTransactions.Where(x => x.OrderId != null && ids.Contains(x.OrderId.Value) && x.Kind == InventoryKind.Return).SumAsync(x => (decimal?)(x.Quantity * x.InventoryBatch.Cost)) ?? 0;
            var cogs = orders.SelectMany(x => x.Items).SelectMany(x => x.Allocations).Sum(x => x.Quantity * x.InventoryBatch.Cost) - returnedCost;
            var policy = await pricing.PolicyAsync();
            var liability = await db.TokenLedger.SumAsync(x => (decimal?)(x.PendingDelta + x.AvailableDelta + x.ReservedDelta)) ?? 0;
            m.Metrics = new() { ["GMV"] = N(gross), ["ยอดขายสุทธิ (หักคืนเงิน)"] = N(net), ["คำสั่งซื้อ"] = orders.Count.ToString(), ["AOV"] = N(orders.Count == 0 ? 0 : net / orders.Count), ["COGS"] = N(cogs), ["กำไรขั้นต้น"] = N(net - cogs), ["Contribution margin"] = N(net - cogs - orders.Sum(x => x.ChannelFee + x.AffiliateFee)), ["ค่าธรรมเนียมช่องทาง"] = N(orders.Sum(x => x.ChannelFee)), ["Affiliate fees"] = N(orders.Sum(x => x.AffiliateFee)), ["Token liability THB"] = N(liability * policy.RedemptionReferenceThb), ["สมาชิกใช้งาน"] = (await db.Members.CountAsync(x => x.Status == MemberStatus.Active)).ToString(), ["สมาชิกใหม่"] = (await db.Members.CountAsync(x => x.CreatedAt >= start && x.CreatedAt < end)).ToString(), ["ผู้ขายที่มียอด"] = orders.Where(x => x.SellerMemberId != null).Select(x => x.SellerMemberId).Distinct().Count().ToString(), ["ลูกค้าซื้อซ้ำในช่วง"] = orders.Where(x => x.BuyerMemberId != null).GroupBy(x => x.BuyerMemberId).Count(x => x.Count() > 1).ToString() };
            m.Headers = ["ช่องทาง", "คำสั่งซื้อ", "ยอดสินค้า", "ส่วนลด", "ค่าธรรมเนียม"];
            m.Rows = orders.GroupBy(x => x.Channel).Select(g => new TableRow([g.Key.ToString(), g.Count().ToString(), N(g.Sum(x => x.Merchandise)), N(g.Sum(x => x.Discount)), N(g.Sum(x => x.ChannelFee + x.AffiliateFee))])).ToList();
            m.Metrics["คืนเงินสด"] = N(refunds);
            m.Metrics["เงินรับจาก POS"] = N(orders.Where(x => x.Channel == SalesChannel.POS).Sum(x => x.CashPayable));
            m.Metrics["ลูกค้าใหม่ในช่วง"] = (await db.Members.CountAsync(x => x.CreatedAt >= start && x.CreatedAt < end && db.Orders.Any(o => o.BuyerMemberId == x.Id))).ToString();
            m.Metrics["สินค้าล็อตใกล้หมดอายุ 90 วัน"] = (await db.InventoryBatches.CountAsync(x => x.QtyAvailable > 0 && x.ExpDate < DateTime.UtcNow.AddDays(90))).ToString();
            var balanceGroups = await db.TokenLedger.GroupBy(x => x.MemberId).Select(g => new { Pending = g.Sum(x => x.PendingDelta), Available = g.Sum(x => x.AvailableDelta), Reserved = g.Sum(x => x.ReservedDelta) }).ToListAsync();
            m.Metrics["Token pending"] = N(balanceGroups.Sum(x => x.Pending)); m.Metrics["Token available"] = N(balanceGroups.Sum(x => x.Available)); m.Metrics["Token reserved"] = N(balanceGroups.Sum(x => x.Reserved));
            m.Metrics["Token liability THB"] = N(balanceGroups.Sum(x => x.Pending + Math.Max(0, x.Available) + x.Reserved) * policy.RedemptionReferenceThb);
            m.Metrics["Token redeemed"] = N(-(await db.TokenLedger.Where(x => x.Kind == LedgerKind.REDEMPTION).SumAsync(x => (decimal?)x.ReservedDelta) ?? 0));
            m.Metrics["Token issued"] = N(await db.TokenDistributions.SumAsync(x => (decimal?)x.Amount) ?? 0);
            m.Metrics["Token reversed"] = N(await db.TokenDistributions.SumAsync(x => (decimal?)x.ReversedAmount) ?? 0);
            m.Metrics["Token expired"] = N(-(await db.TokenLedger.Where(x => x.Kind == LedgerKind.EXPIRY).SumAsync(x => (decimal?)x.AvailableDelta) ?? 0));
            if (q?.Equals("SKU", StringComparison.OrdinalIgnoreCase) == true)
            { m.Headers = ["SKU ID", "สินค้า", "จำนวนสุทธิ", "ยอดสินค้าสุทธิ"]; m.Rows = orders.SelectMany(x => x.Items).GroupBy(x => new { x.SkuId, x.Name }).Select(g => new TableRow([g.Key.SkuId.ToString(), g.Key.Name, g.Sum(x => x.Quantity - x.ReturnedQuantity).ToString(), N(g.Sum(x => (x.UnitPrice * x.Quantity - x.Discount) * (x.Quantity - x.ReturnedQuantity) / x.Quantity))])).ToList(); }
            else if (q?.Equals("Campaign", StringComparison.OrdinalIgnoreCase) == true)
            { m.Headers = ["Campaign", "คำสั่งซื้อ", "ยอดสินค้า"]; m.Rows = orders.GroupBy(x => x.Campaign).Select(g => new TableRow([string.IsNullOrEmpty(g.Key) ? "ไม่มีแคมเปญ" : g.Key, g.Count().ToString(), N(g.Sum(x => x.Merchandise - x.Discount))])).ToList(); }
            else if (q?.Equals("Seller", StringComparison.OrdinalIgnoreCase) == true)
            { m.Headers = ["Seller Member ID", "คำสั่งซื้อ", "ยอดสินค้า"]; m.Rows = orders.GroupBy(x => x.SellerMemberId).Select(g => new TableRow([g.Key?.ToString() ?? "ไม่มีผู้แนะนำ", g.Count().ToString(), N(g.Sum(x => x.Merchandise - x.Discount))])).ToList(); }
            else if (q?.Equals("Upline", StringComparison.OrdinalIgnoreCase) == true)
            { m.Headers = ["Level", "ออก Token", "ย้อนคืน", "คงเหลือก่อนหมดอายุ"]; m.Rows = (await db.TokenDistributions.Where(x => ids.Contains(x.SourceOrderId)).ToListAsync()).GroupBy(x => x.Level).Select(g => new TableRow([g.Key.ToString(), N(g.Sum(x => x.Amount)), N(g.Sum(x => x.ReversedAmount)), N(g.Sum(x => x.Amount - x.ReversedAmount))])).ToList(); }
            foreach (var group in await db.TokenLedger.GroupBy(x => x.Kind).Select(g => new { Kind = g.Key, Amount = g.Sum(x => x.PendingDelta + x.AvailableDelta + x.ReservedDelta) }).ToListAsync()) m.Metrics["Token " + group.Kind] = N(group.Amount);
        }
        else if (section is "Members" or "Tree")
        {
            m.Headers = ["ID", "รหัส", "ชื่อ", "ผู้แนะนำ", "ประเภท", "สถานะ", "ตรวจสอบยอดติดลบ"];
            var data = await db.Members.Where(x => q == null || x.Code.Contains(q) || x.Name.Contains(q)).OrderBy(x => x.Id).Skip(skip).Take(100).ToListAsync();
            m.Rows = data.Select(x => new TableRow([x.Id.ToString(), x.Code, x.Name, x.SponsorMemberId?.ToString() ?? "—", x.Type.ToString(), x.Status.ToString(), x.ReviewRequired.ToString()])).ToList();
            if (section == "Tree") m.Tree = await db.Members.Select(x => new TreeNode(x.Id, x.SponsorMemberId, x.Code, x.Name, x.SponsorMemberId == null ? 0 : 1, x.Status)).ToListAsync();
        }
        else if (section == "Products")
        {
            m.Headers = ["SKU ID", "SKU", "สินค้า", "ตัวเลือก", "หมวดหมู่", "สถานะ"];
            m.Rows = (await db.Skus.Include(x => x.Product).Where(x => q == null || x.Code.Contains(q) || x.Product.Name.Contains(q)).OrderBy(x => x.Id).Skip(skip).Take(100).ToListAsync()).Select(x => new TableRow([x.Id.ToString(), x.Code, x.Product.Name, x.Variant, x.Product.Category, x.Active.ToString()])).ToList();
        }
        else if (section == "Prices")
        {
            m.Headers = ["Version", "SKU", "Tier", "จำนวน/แพ็ก", "ราคา", "ช่องทาง", "เริ่ม UTC", "สิ้นสุด UTC"];
            m.Rows = (await db.ProductPrices.Include(x => x.Sku).OrderByDescending(x => x.EffectiveFrom).Skip(skip).Take(100).ToListAsync()).Select(x => new TableRow([x.Id.ToString(), x.Sku.Code, x.Tier, x.PackQuantity.ToString(), N(x.Amount), x.Channel?.ToString() ?? "ทุกช่องทาง", x.EffectiveFrom.ToString("u"), x.EffectiveTo?.ToString("u") ?? "—"])).ToList();
        }
        else if (section == "TokenRates")
        {
            m.Headers = ["Version", "SKU", "Base Token", "ช่องทาง", "เริ่ม UTC"];
            m.Rows = (await db.TokenRates.Include(x => x.Sku).OrderByDescending(x => x.EffectiveFrom).Skip(skip).Take(100).ToListAsync()).Select(x => new TableRow([x.Id.ToString(), x.Sku.Code, N(x.BaseToken), x.Channel?.ToString() ?? "ทุกช่องทาง", x.EffectiveFrom.ToString("u")])).ToList();
        }
        else if (section == "TokenPolicies")
        {
            m.Policy = await pricing.PolicyAsync(); m.Headers = ["Version", "เริ่ม UTC", "Seller %", "L1 %", "L2 %", "L3 %", "รอ/วัน", "เหตุผล"];
            m.Rows = (await db.TokenPolicies.OrderByDescending(x => x.EffectiveFrom).ToListAsync()).Select(x => new TableRow([x.Id.ToString(), x.EffectiveFrom.ToString("u"), N(x.SellerPercent), N(x.Level1Percent), N(x.Level2Percent), N(x.Level3Percent), x.PendingDays.ToString(), x.Reason])).ToList();
        }
        else if (section == "Ledger")
        {
            m.Headers = ["ID", "Member", "ประเภท", "Pending", "Available", "Reserved", "เวลา"];
            m.Rows = (await db.TokenLedger.OrderByDescending(x => x.Id).Skip(skip).Take(100).ToListAsync()).Select(x => new TableRow([x.Id.ToString(), x.MemberId.ToString(), x.Kind.ToString(), N(x.PendingDelta), N(x.AvailableDelta), N(x.ReservedDelta), Bangkok.Format(x.PostedAt)])).ToList();
        }
        else if (section == "Inventory")
        {
            m.Headers = ["Batch ID", "SKU", "คลัง", "ล็อต", "คงเหลือ", "จอง", "หมดอายุ", "ต้นทุน"];
            m.Rows = (await db.InventoryBatches.Include(x => x.Sku).Include(x => x.Warehouse).OrderBy(x => x.ExpDate).Skip(skip).Take(100).ToListAsync()).Select(x => new TableRow([x.Id.ToString(), x.Sku.Code, x.Warehouse.Name, x.LotNo, x.QtyAvailable.ToString(), x.QtyReserved.ToString(), Bangkok.Format(x.ExpDate), N(x.Cost)])).ToList();
        }
        else if (section == "StockCard")
        {
            m.Headers = ["เวลา", "ล็อต", "ชนิด", "จำนวน", "Order ID", "เหตุผล"];
            m.Rows = (await db.InventoryTransactions.Include(x => x.InventoryBatch).OrderByDescending(x => x.Id).Skip(skip).Take(100).ToListAsync()).Select(x => new TableRow([Bangkok.Format(x.PostedAt), x.InventoryBatch.LotNo, x.Kind.ToString(), x.Quantity.ToString(), x.OrderId?.ToString() ?? "—", x.Reason])).ToList();
        }
        else if (section == "Orders")
        {
            m.Headers = ["ID", "เลขคำสั่งซื้อ", "ช่องทาง", "ลูกค้า", "สถานะ", "ยอดเงิน", "เวลา"];
            m.Rows = (await db.Orders.Where(x => q == null || x.Number.Contains(q) || x.CustomerName.Contains(q)).OrderByDescending(x => x.Id).Skip(skip).Take(100).ToListAsync()).Select(x => new TableRow([x.Id.ToString(), x.Number, x.Channel.ToString(), x.CustomerName, x.Status.ToString(), N(x.CashPayable), Bangkok.Format(x.CreatedAt)], "/Orders/Detail/" + x.PublicId)).ToList();
        }
        else if (section == "Payments")
        {
            m.Headers = ["ID", "Order ID", "วิธี", "ยอด", "สถานะ", "อ้างอิง"];
            m.Rows = (await db.Payments.OrderByDescending(x => x.Id).Skip(skip).Take(100).ToListAsync()).Select(x => new TableRow([x.Id.ToString(), x.OrderId.ToString(), x.Provider, N(x.Amount), x.Status, x.TransactionRef ?? "—"])).ToList();
        }
        else if (section == "Returns")
        {
            m.Headers = ["ID", "Item ID", "จำนวน", "เหตุผล", "สถานะ", "คืนเงิน", "คืน Token"];
            m.Rows = (await db.ReturnRequests.OrderByDescending(x => x.Id).Skip(skip).Take(100).ToListAsync()).Select(x => new TableRow([x.Id.ToString(), x.OrderItemId.ToString(), x.Quantity.ToString(), x.Reason, x.Status, N(x.CashRefund), N(x.TokensRestored)])).ToList();
        }
        else if (section == "Promotions")
        {
            m.Headers = ["ID", "คูปอง", "ชนิด", "มูลค่า", "ใช้แล้ว/จำกัด", "เริ่ม UTC", "สิ้นสุด UTC"];
            m.Rows = (await db.Promotions.OrderByDescending(x => x.Id).Skip(skip).Take(100).ToListAsync()).Select(x => new TableRow([x.Id.ToString(), x.Code, x.Kind.ToString(), N(x.Value), $"{x.UsedCount}/{x.UsageLimit}", x.EffectiveFrom.ToString("u"), x.EffectiveTo.ToString("u")])).ToList();
        }
        else if (section == "POS")
        {
            m.Headers = ["ID", "User ID", "ชื่อจุดขาย", "คลัง", "เปิดใช้งาน"];
            m.Rows = (await db.PosRegisters.Include(x => x.Warehouse).OrderBy(x => x.Id).Skip(skip).Take(100).ToListAsync()).Select(x => new TableRow([x.Id.ToString(), x.UserId, x.Name, x.Warehouse.Name, x.Enabled.ToString()])).ToList();
            var sessions = await db.PosSessions.Where(x => x.ClosedAt != null).ToListAsync();
            m.Metrics["กะปิดแล้ว"] = sessions.Count.ToString(); m.Metrics["ผลต่างเงินรวม"] = N(sessions.Sum(x => x.Difference ?? 0));
        }
        else if (section == "Settings")
        {
            m.Headers = ["Key", "Value"];
            m.Rows = (await db.SystemSettings.Where(x => !x.Key.StartsWith("Demo.")).OrderBy(x => x.Key).ToListAsync()).Select(x => new TableRow([x.Key, x.Value])).ToList();
            foreach (var p in await db.ChannelPolicies.ToListAsync()) m.Metrics[p.Channel.ToString()] = $"Network={p.AllowNetworkReward}; Redeem={p.AllowRedemption}";
        }
        else if (section == "AuditLogs")
        {
            m.Headers = ["เวลา", "ผู้ทำ", "การทำรายการ", "เป้าหมาย", "รายละเอียด"];
            m.Rows = (await db.AuditLogs.OrderByDescending(x => x.Id).Skip(skip).Take(100).ToListAsync()).Select(x => new TableRow([Bangkok.Format(x.CreatedAt), x.ActorId, x.Action, x.Subject, x.Detail])).ToList();
        }
        else if (section == "Warehouses")
        { m.Headers = ["ID", "ชื่อ", "สถานะ"]; m.Rows = (await db.Warehouses.ToListAsync()).Select(x => new TableRow([x.Id.ToString(), x.Name, x.Active.ToString()])).ToList(); }
        else if (section == "Notifications")
        { m.Headers = ["เวลา", "ข้อความ"]; m.Rows = (await db.Notifications.Where(x => x.UserId == null).OrderByDescending(x => x.Id).Take(100).ToListAsync()).Select(x => new TableRow([Bangkok.Format(x.CreatedAt), x.Message])).ToList(); }
        m.Skus = await db.Skus.OrderBy(x => x.Code).ToListAsync(); m.Warehouses = await db.Warehouses.Where(x => x.Active).ToListAsync(); return m;
    }
    private IActionResult Back(string section) { TempData["Success"] = "บันทึกสำเร็จ"; return RedirectToAction("Index", new { section }); }
    [Authorize(Policy = "Prices")] public async Task<IActionResult> Product(long? id)
    {
        if (id == null) return View(new ProductInput());
        var s = await db.Skus.Include(x => x.Product).SingleOrDefaultAsync(x => x.Id == id); if (s == null) return NotFound(); var p = s.Product;
        return View(new ProductInput { SkuId = s.Id, ProductId = s.ProductId, Code = s.Code, Barcode = s.Barcode, Variant = s.Variant, CapsuleCount = s.CapsuleCount, WeightGrams = s.WeightGrams, LowStockThreshold = s.LowStockThreshold, Active = s.Active, TokenEligible = s.TokenEligible, Name = p.Name, Category = p.Category, Description = p.Description, ImageUrl = p.ImageUrl, RegistrationNumber = p.RegistrationNumber, Manufacturer = p.Manufacturer, Distributor = p.Distributor, Warnings = p.Warnings, LabelDocumentUrl = p.LabelDocumentUrl, LotRequired = p.LotRequired, InitialPrice = (await pricing.PriceAsync(s.Id, SalesChannel.Store)).Amount, InitialToken = (await pricing.RateAsync(s.Id, SalesChannel.Store)).BaseToken, RowVersion = Convert.ToBase64String(s.RowVersion) });
    }
    [HttpPost, Authorize(Policy = "Prices")] public async Task<IActionResult> Product(ProductInput input)
    {
        if (!ModelState.IsValid) return View(input);
        static bool SafeUrl(string? url) => string.IsNullOrWhiteSpace(url) || (url.StartsWith('/') && !url.StartsWith("//")) || (Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == "https");
        if (!SafeUrl(input.ImageUrl) || !SafeUrl(input.LabelDocumentUrl)) throw new BusinessException("URL ต้องเป็น HTTPS หรือเส้นทางในระบบ");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var sku = input.SkuId == null ? new Sku() : await db.Skus.Include(x => x.Product).SingleAsync(x => x.Id == input.SkuId);
        if (input.SkuId == null)
        {
            sku.Product = input.ProductId == null ? new Product() : await db.Products.SingleAsync(x => x.Id == input.ProductId);
            db.Skus.Add(sku);
        }
        else if (input.RowVersion == null || !Convert.ToBase64String(sku.RowVersion).Equals(input.RowVersion)) throw new BusinessException("ข้อมูลสินค้าเปลี่ยนแล้ว กรุณาโหลดใหม่");
        var p = sku.Product; p.Name = input.Name; p.Category = input.Category ?? ""; p.Description = input.Description ?? ""; p.ImageUrl = input.ImageUrl ?? "";
        p.RegistrationNumber = input.RegistrationNumber ?? ""; p.Manufacturer = input.Manufacturer ?? ""; p.Distributor = input.Distributor ?? "";
        p.Warnings = input.Warnings ?? ""; p.LabelDocumentUrl = input.LabelDocumentUrl ?? ""; p.LotRequired = input.LotRequired;
        sku.Code = input.Code; sku.Barcode = input.Barcode ?? ""; sku.Variant = input.Variant; sku.Active = input.Active; sku.TokenEligible = input.TokenEligible;
        sku.CapsuleCount = input.CapsuleCount; sku.WeightGrams = input.WeightGrams; sku.LowStockThreshold = input.LowStockThreshold;
        if (input.SkuId == null)
        {
            db.ProductPrices.Add(new ProductPrice { Sku = sku, Amount = input.InitialPrice, EffectiveFrom = DateTime.UtcNow });
            db.TokenRates.Add(new TokenRate { Sku = sku, BaseToken = input.InitialToken, EffectiveFrom = DateTime.UtcNow });
        }
        audit.Add("Product.Save", input.Code, input.Reason); await db.SaveChangesAsync(); await tx.CommitAsync();
        TempData["Success"]="บันทึกข้อมูลสินค้าแล้ว สามารถจัดการรูปภาพด้านล่างได้";
        return RedirectToAction(nameof(Product),new{id=sku.Id});
    }
    [HttpPost, Authorize(Policy = "Prices")] public async Task<IActionResult> Promotion(PromotionInput input)
    {
        if (!ModelState.IsValid || !Enum.IsDefined(input.Kind) || input.EffectiveTo <= input.EffectiveFrom || (input.Kind == PromotionKind.Percentage && input.Value > 100) || (input.Kind == PromotionKind.FreeGift && input.GiftSkuId == null)) throw new BusinessException("เงื่อนไขโปรโมชันไม่ถูกต้อง");
        db.Promotions.Add(new Promotion { Code = input.Code.ToUpperInvariant(), Kind = input.Kind, Value = input.Value, MinimumSpend = input.MinimumSpend, RequiredQuantity = input.RequiredQuantity, EligibleSkuId = input.EligibleSkuId, GiftSkuId = input.GiftSkuId, Channel = input.Channel, MemberType = input.MemberType, UsageLimit = input.UsageLimit, EffectiveFrom = DateTime.SpecifyKind(input.EffectiveFrom, DateTimeKind.Utc), EffectiveTo = DateTime.SpecifyKind(input.EffectiveTo, DateTimeKind.Utc), TokenMultiplier = input.TokenMultiplier, Reason = input.Reason });
        audit.Add("Promotion.Create", input.Code, input.Reason); await db.SaveChangesAsync(); return Back("Promotions");
    }
    [HttpPost, Authorize(Policy = "Prices")] public async Task<IActionResult> DisablePromotion(long id, string reason)
    { (await db.Promotions.SingleAsync(x => x.Id == id)).Active = false; audit.Add("Promotion.Disable", id, reason); await db.SaveChangesAsync(); return Back("Promotions"); }
    [HttpPost, Authorize(Policy = "Payments")] public async Task<IActionResult> ConfirmPayment(long orderId, string reference, decimal amount, string reason) { await commerce.ConfirmPaymentAsync(orderId, reference, amount, reason); return Back("Payments"); }
    [HttpPost, Authorize(Policy = "Payments")] public async Task<IActionResult> VerifyRetail(long orderId, string reason) { await commerce.VerifyRetailAsync(orderId, reason); return Back("Orders"); }
    [HttpPost, Authorize(Policy = "Orders")] public async Task<IActionResult> Fulfill(long orderId, string carrier, string tracking, bool delivered, string reason) { await commerce.FulfillAsync(orderId, carrier, tracking, delivered, reason); return Back("Orders"); }
    [HttpPost, Authorize(Policy = "Refunds")] public async Task<IActionResult> ApproveReturn(long requestId, bool sellable, string reference, string reason) { await commerce.ApproveReturnAsync(requestId, sellable, reference, reason); return Back("Returns"); }
    [HttpPost, Authorize(Policy = "Inventory")] public async Task<IActionResult> Receive(long skuId, long warehouseId, string lot, DateTime mfg, DateTime expiry, int quantity, decimal cost, string reason) { await inventory.ReceiveAsync(skuId, warehouseId, lot, DateTime.SpecifyKind(mfg, DateTimeKind.Utc), DateTime.SpecifyKind(expiry, DateTimeKind.Utc), quantity, cost, reason); return Back("Inventory"); }
    [HttpPost, Authorize(Policy = "Inventory")] public async Task<IActionResult> AdjustStock(long batchId, int delta, InventoryKind kind, string reason) { await inventory.AdjustAsync(batchId, delta, kind, reason); return Back("Inventory"); }
    [HttpPost, Authorize(Policy = "Members")] public async Task<IActionResult> Sponsor(long memberId, long? sponsorId, string reason) { if (string.IsNullOrWhiteSpace(reason)) throw new BusinessException("กรุณาระบุเหตุผล"); await members.MoveAsync(memberId, sponsorId, reason); return Back("Members"); }
    [HttpPost, Authorize(Policy = "Members")] public async Task<IActionResult> SetMemberStatus(long memberId, MemberStatus status, MemberType type, string reason)
    { if (!Enum.IsDefined(status) || !Enum.IsDefined(type)) throw new BusinessException("สถานะไม่ถูกต้อง"); var m = await db.Members.SingleAsync(x => x.Id == memberId); m.Status = status; m.Type = type; audit.Add("Member.Status", memberId, reason); await db.SaveChangesAsync(); return Back("Members"); }
    [HttpPost, Authorize(Policy = "Prices")] public async Task<IActionResult> Price(long skuId, string tier, int packQuantity, decimal amount, SalesChannel? channel, DateTime effectiveFrom, string reason)
    {
        if (amount <= 0 || packQuantity < 1 || packQuantity > 1000 || Money.Round(amount / packQuantity) * packQuantity != amount || !new[] { "Retail", "Promo", "Wholesale", "Pack6", "Pack12" }.Contains(tier) || effectiveFrom < DateTime.UtcNow.AddMinutes(-5)) throw new BusinessException("ข้อมูลราคาหรือเวลาเริ่มมีผลไม่ถูกต้อง");
        db.ProductPrices.Add(new ProductPrice { SkuId = skuId, Tier = tier, PackQuantity = packQuantity, Amount = amount, Channel = channel, EffectiveFrom = DateTime.SpecifyKind(effectiveFrom, DateTimeKind.Utc) });
        audit.Add("Price.Version", skuId, reason); await db.SaveChangesAsync(); return Back("Prices");
    }
    [HttpPost, Authorize(Policy = "Tokens")] public async Task<IActionResult> TokenRate(long skuId, decimal amount, SalesChannel? channel, DateTime effectiveFrom, string reason)
    {
        if (amount < 0 || effectiveFrom < DateTime.UtcNow.AddMinutes(-5)) throw new BusinessException("ข้อมูลอัตรา Token ไม่ถูกต้อง");
        db.TokenRates.Add(new TokenRate { SkuId = skuId, BaseToken = amount, Channel = channel, EffectiveFrom = DateTime.SpecifyKind(effectiveFrom, DateTimeKind.Utc) }); audit.Add("TokenRate.Version", skuId, reason); await db.SaveChangesAsync(); return Back("TokenRates");
    }
    [HttpPost, Authorize(Policy = "Tokens")] public async Task<IActionResult> Policy(int pendingDays, int expiryMonths, int maxDepth, decimal sellerPercent, decimal level1, decimal level2, decimal level3, decimal reference, decimal maxRedemption, bool loyalty, decimal loyaltyPercent, DateTime effectiveFrom, string reason, string? redemptionCategories)
    {
        if (pendingDays is < 0 or > 60 || expiryMonths < 1 || maxDepth is < 0 or > 3 || reference <= 0 || maxRedemption is < 0 or > 100 || new[] { sellerPercent, level1, level2, level3, loyaltyPercent }.Any(x => x < 0 || x > 100) || effectiveFrom < DateTime.UtcNow.AddMinutes(-5)) throw new BusinessException("นโยบาย Token ไม่ถูกต้อง");
        if (redemptionCategories?.Length > 1000) throw new BusinessException("หมวดหมู่ยาวเกินไป");
        db.TokenPolicies.Add(new TokenPolicy { PendingDays = pendingDays, ExpiryMonths = expiryMonths, MaxDepth = maxDepth, SellerPercent = sellerPercent, Level1Percent = level1, Level2Percent = level2, Level3Percent = level3, RedemptionReferenceThb = reference, MaxRedemptionPercent = maxRedemption, LoyaltyEnabled = loyalty, LoyaltyPercent = loyaltyPercent, EffectiveFrom = DateTime.SpecifyKind(effectiveFrom, DateTimeKind.Utc), Reason = reason, RedemptionCategories = redemptionCategories ?? "" });
        audit.Add("TokenPolicy.Version", "new", reason); await db.SaveChangesAsync(); return Back("TokenPolicies");
    }
    [HttpPost, Authorize(Policy = "Tokens")] public async Task<IActionResult> AdjustToken(long memberId, decimal amount, string reason, string key)
    {
        if (amount == 0 || Money.Round(amount) != amount || string.IsNullOrWhiteSpace(key)) throw new BusinessException("ข้อมูลปรับ Token ไม่ถูกต้อง");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        if (await db.TokenLedger.AnyAsync(x => x.EventKey == "adjust:" + key)) return Back("Ledger");
        db.TokenLedger.Add(new TokenLedger { MemberId = memberId, Kind = LedgerKind.ADMIN_ADJUSTMENT, Status = TokenStatus.Available, AvailableDelta = amount, EventKey = "adjust:" + key, Reason = reason });
        audit.Add("Token.Adjust", memberId, reason); await db.SaveChangesAsync(); await tx.CommitAsync(); return Back("Ledger");
    }
    [HttpPost, Authorize(Policy = "Settings")] public async Task<IActionResult> Register(long registerId, long warehouseId, bool enabled, string reason)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        if (await db.PosSessions.AnyAsync(x => x.PosRegisterId == registerId && x.ClosedAt == null)) throw new BusinessException("ปิดกะก่อนเปลี่ยนจุดขาย");
        if (!await db.Warehouses.AnyAsync(x => x.Id == warehouseId && x.Active)) throw new BusinessException("คลังไม่พร้อมใช้");
        var r = await db.PosRegisters.SingleAsync(x => x.Id == registerId); if (!await db.Warehouses.AnyAsync(x => x.Id == warehouseId && (x.OwnerMemberId == null || x.OwnerMember!.UserId == r.UserId))) throw new BusinessException("จุดขายต้องใช้คลังบริษัทหรือคลังของเจ้าของจุดขายเท่านั้น"); r.WarehouseId = warehouseId; r.Enabled = enabled; audit.Add("POS.Configure", registerId, reason); await db.SaveChangesAsync(); await tx.CommitAsync(); return Back("POS");
    }
    [HttpPost, Authorize(Policy = "Settings")] public async Task<IActionResult> Setting(string key, int value, string reason)
    {
        if (key is not ("ReferralCookieDays" or "AbandonedCheckoutHours") || value is < 1 or > 365) throw new BusinessException("ค่าไม่รองรับ เปลี่ยนนโยบาย Token ผ่านหน้าสร้างเวอร์ชันนโยบาย");
        (await db.SystemSettings.SingleAsync(x => x.Key == key)).Value = value.ToString(CultureInfo.InvariantCulture); audit.Add("Setting.Change", key, reason); await db.SaveChangesAsync(); return Back("Settings");
    }
    [HttpPost, Authorize(Policy = "Settings")] public async Task<IActionResult> Tax(decimal percent, string reason)
    { if (percent < 0 || percent > 100) throw new BusinessException("อัตราภาษีไม่ถูกต้อง"); (await db.SystemSettings.SingleAsync(x => x.Key == "TaxRatePercent")).Value = percent.ToString(CultureInfo.InvariantCulture); audit.Add("Tax.Change", "TaxRatePercent", reason); await db.SaveChangesAsync(); return Back("Settings"); }
    [HttpPost, Authorize(Policy = "Tokens")] public async Task<IActionResult> ClearTokenReview(long memberId, string reason)
    { await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable); var total = await db.TokenLedger.Where(x => x.MemberId == memberId).SumAsync(x => (decimal?)x.AvailableDelta) ?? 0; if (total < 0) throw new BusinessException("ยอด Token ยังติดลบ"); (await db.Members.SingleAsync(x => x.Id == memberId)).ReviewRequired = false; audit.Add("Token.ClearReview", memberId, reason); await db.SaveChangesAsync(); await tx.CommitAsync(); return Back("Ledger"); }
    [HttpPost, Authorize(Policy = "Settings")] public async Task<IActionResult> Channel(SalesChannel channel, bool network, bool redemption, string reason)
    {
        var p = await db.ChannelPolicies.SingleAsync(x => x.Channel == channel); p.AllowNetworkReward = network; p.AllowRedemption = redemption; audit.Add("Channel.Policy", channel, reason); await db.SaveChangesAsync(); return Back("Settings");
    }
    [HttpPost, Authorize(Roles = "SuperAdmin")] public async Task<IActionResult> Role(string email, string role, bool grant, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason)) throw new BusinessException("กรุณาระบุเหตุผลเปลี่ยนสิทธิ์");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        if (!SeedData.Roles.Contains(role)) throw new BusinessException("ไม่พบสิทธิ์");
        var user = await users.FindByEmailAsync(email) ?? throw new BusinessException("ไม่พบผู้ใช้");
        if (user.Id == actor.Id && !grant) throw new BusinessException("ไม่สามารถถอนสิทธิ์ของตัวเอง");
        audit.Add("User.Role", user.Id, role + ":" + grant + "; " + reason);
        var result = grant ? await users.AddToRoleAsync(user, role) : await users.RemoveFromRoleAsync(user, role);
        if (!result.Succeeded) throw new BusinessException("เปลี่ยนสิทธิ์ไม่สำเร็จ");
        await users.UpdateSecurityStampAsync(user); await db.SaveChangesAsync(); await tx.CommitAsync(); return Back("Settings");
    }
    [HttpPost, Authorize(Policy = "Inventory")] public async Task<IActionResult> Warehouse(string name, string reason)
    { if (string.IsNullOrWhiteSpace(name) || name.Length > 120) throw new BusinessException("ชื่อคลังไม่ถูกต้อง"); db.Warehouses.Add(new Warehouse { Name = name }); audit.Add("Warehouse.Create", name, reason); await db.SaveChangesAsync(); return Back("Warehouses"); }
    [HttpPost, Authorize(Policy = "Prices")] public async Task<IActionResult> Shipping(decimal fee, decimal freeAbove, string reason)
    { if (fee < 0 || freeAbove < 0) throw new BusinessException("ค่าจัดส่งไม่ถูกต้อง"); var s = await db.ShippingRules.FirstAsync(); s.Fee = fee; s.FreeAbove = freeAbove; audit.Add("Shipping.Change", s.Id, reason); await db.SaveChangesAsync(); return Back("Products"); }
    [HttpPost, Authorize(Policy = "Payments"), RequestSizeLimit(5_000_000)] public async Task<IActionResult> Import(IFormFile file)
    { if (file == null || file.Length == 0) throw new BusinessException("กรุณาเลือกไฟล์ CSV"); await using var stream = file.OpenReadStream(); try { return View("ImportResults", await importer.ImportAsync(stream, actor.Id)); } catch (CsvHelper.CsvHelperException) { throw new BusinessException("รูปแบบ CSV ไม่ถูกต้อง ตรวจสอบหัวคอลัมน์และชนิดข้อมูลตามแม่แบบ"); } }
    [Authorize(Policy = "Reports")] public async Task<IActionResult> Export(string section = "Reports", DateTime? from = null, DateTime? to = null, string? q = null)
    {
        if (!(await authorization.AuthorizeAsync(User, Permission(section))).Succeeded) return Forbid();
        var page = await PageAsync(section, q, 1, from, to);
        if (section is not ("Reports" or "Dashboard" or "Settings" or "TokenPolicies" or "Warehouses" or "Notifications"))
        { for (var next = 2; page.Rows.Count == (next - 1) * 100; next++) { var more = await PageAsync(section, q, next, from, to); page.Rows.AddRange(more.Rows); if (more.Rows.Count < 100) break; } }
        static string Cell(string s) { if (s.Length > 0 && "=+-@\t\r".Contains(s[0])) s = "'" + s; return "\"" + s.Replace("\"", "\"\"") + "\""; }
        var text = string.Join(",", page.Headers.Select(Cell)) + "\r\n" + string.Join("\r\n", page.Rows.Select(x => string.Join(",", x.Cells.Select(Cell))));
        return File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(text)).ToArray(), "text/csv; charset=utf-8", "amherb-" + section + ".csv");
    }
}
