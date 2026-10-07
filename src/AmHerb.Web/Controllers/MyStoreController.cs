using System.Text;
using AmHerb.Web.Data;
using AmHerb.Web.Domain;
using AmHerb.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;
namespace AmHerb.Web.Controllers;

[Authorize]
public class MyStoreController(AmHerbDbContext db, MemberStoreService stores, MemberService members, StoreQueries queries, CommerceService commerce, Actor actor) : Controller
{
    public async Task<IActionResult> Index()
    {
        var member = await members.RequireAsync(actor.Id);
        var store = await db.MemberStores.Include(x => x.Warehouse).SingleOrDefaultAsync(x => x.MemberId == member.Id);
        if (store == null) return View(new MyStorePage());
        var orders = db.Orders.Where(x => x.StoreId == store.Id || (x.Channel == SalesChannel.POS && x.CashierUserId == actor.Id));
        return View(new MyStorePage { Store = store, Pending = await orders.CountAsync(x => x.Status == OrderStatus.PendingPayment), ReadyToShip = await orders.CountAsync(x => x.StoreId == store.Id && (x.Status == OrderStatus.Paid || x.Status == OrderStatus.Processing)), Orders = await orders.OrderByDescending(x => x.Id).Take(50).ToListAsync() });
    }
    [HttpPost] public async Task<IActionResult> Create() { await stores.CreateAsync(actor.Id); return RedirectToAction(nameof(Index)); }
    [HttpPost] public async Task<IActionResult> Save(string name, string? description, string? phone, bool published) { await stores.SaveAsync(actor.Id, name ?? "", description ?? "", phone ?? "", published); TempData["Success"] = "บันทึกร้านแล้ว"; return RedirectToAction(nameof(Index)); }
    public async Task<IActionResult> Products(string? q)
    {
        var store = await stores.RequireAsync(actor.Id);
        return View(new StoreProductsPage(store, await queries.CatalogAsync(q, warehouse: store.WarehouseId), await db.StoreProducts.Where(x => x.StoreId == store.Id && x.Enabled).Select(x => x.SkuId).ToListAsync(), q));
    }
    [HttpPost] public async Task<IActionResult> Product(long skuId, bool enabled) { await stores.SetProductAsync(actor.Id, skuId, enabled); return RedirectToAction(nameof(Products)); }
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Orders(OrderStatus? status, int page = 1)
    {
        var store = await stores.RequireAsync(actor.Id); page = Math.Clamp(page, 1, 100000);
        ViewBag.Carriers = (await new DeliveryService(db).Options(store.Id)).Where(x => x.Enabled).ToList();

        var query = db.Orders.Where(x => (x.StoreId == store.Id || (x.Channel == SalesChannel.POS && x.CashierUserId == actor.Id)) && (status == null || x.Status == status));
        ViewBag.Page = page; ViewBag.Status = status; var rows = await query.OrderByDescending(x => x.Id).Skip((page - 1) * 30).Take(31).ToListAsync(); ViewBag.HasNext = rows.Count > 30;
        var orderIds = rows.Take(30).Select(x => x.Id).ToArray();
        ViewBag.Shipments = await db.Shipments.AsNoTracking().Where(x => orderIds.Contains(x.OrderId)).ToListAsync();
        return View(rows.Take(30).ToList());
    }
    [HttpPost] public async Task<IActionResult> Ship(long id, string carrier, string tracking, bool delivered)
    {
        var order = await stores.RequireOrderAsync(actor.Id, id);
        if (order.StoreId == null || (carrier?.Length ?? 0) > 120 || (tracking?.Length ?? 0) > 120) throw new BusinessException("ข้อมูลจัดส่งไม่ถูกต้อง");
        if (!delivered && carrier != order.SelectedCarrier && !(await new DeliveryService(db).Options(order.StoreId)).Any(x => x.Enabled && x.Name == carrier)) throw new BusinessException("เลือกผู้ขนส่งที่ร้านเปิดใช้งาน");
        await commerce.FulfillAsync(id, carrier ?? "", tracking ?? "", delivered, "เจ้าของร้านดำเนินการจัดส่ง");
        return RedirectToAction(nameof(Orders));
    }
    [HttpPost] public async Task<IActionResult> Prepare(long id)
    {
        var order = await stores.RequireOrderAsync(actor.Id, id);
        if (order.StoreId == null) throw new BusinessException("รายการนี้ไม่ใช่คำสั่งซื้อออนไลน์ของร้าน");
        await commerce.PrepareAsync(id);
        return RedirectToAction(nameof(Orders));
    }
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> ShippingLabel(long id)
    {
        var member = await members.RequireAsync(actor.Id);
        var store = await db.MemberStores.AsNoTracking().SingleOrDefaultAsync(x => x.MemberId == member.Id);
        if (store == null) return NotFound();
        var order = await db.Orders.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.StoreId == store.Id);
        if (order == null) return NotFound();
        return View(new ShippingLabelPage(order, store.Name, string.IsNullOrWhiteSpace(store.Phone) ? member.Phone : store.Phone, member.Address));
    }
    public async Task<IActionResult> Shipping()
    {
        var store = await stores.RequireAsync(actor.Id);
        return View(await new DeliveryService(db).Options(store.Id));
    }
    [HttpPost] public async Task<IActionResult> ShippingOption(long providerId, bool enabled)
    {
        var store = await stores.RequireAsync(actor.Id);
        await new DeliveryService(db).Set(store.Id, providerId, enabled);
        return RedirectToAction(nameof(Shipping));
    }
    [HttpPost] public async Task<IActionResult> AddCarrier(string name)
    {
        var store = await stores.RequireAsync(actor.Id);
        await new DeliveryService(db).Add(store.Id, name);
        return RedirectToAction(nameof(Shipping));
    }
    public async Task<IActionResult> Qr()
    {
        var store = await stores.RequireAsync(actor.Id); using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(Url.Action("Index", "Storefront", new { slug = store.Slug }, Request.Scheme)!, QRCodeGenerator.ECCLevel.Q);
        using var code = new PngByteQRCode(data); return File(code.GetGraphic(8), "image/png");
    }
    public async Task<IActionResult> Inventory(long? skuId, int page = 1)
    {
        var store = await stores.RequireAsync(actor.Id); page = Math.Clamp(page, 1, 100000);
        var lots = await db.InventoryBatches.AsNoTracking().Include(x => x.Sku).ThenInclude(x => x.Product)
            .Where(x => x.WarehouseId == store.WarehouseId && (skuId == null || x.SkuId == skuId)).OrderBy(x => x.ExpDate).ToListAsync();
        var movements = await db.InventoryTransactions.AsNoTracking().Include(x => x.InventoryBatch).ThenInclude(x => x.Sku)
            .Where(x => x.InventoryBatch.WarehouseId == store.WarehouseId && (skuId == null || x.InventoryBatch.SkuId == skuId)).OrderByDescending(x => x.Id).Skip((page - 1) * 50).Take(51).ToListAsync();
        return View(new StoreInventoryPage(store, lots, movements.Take(50).ToList(), skuId, page, movements.Count > 50));
    }
    private async Task<StoreAccountingPage> Report(DateTime? from, DateTime? to)
    {
        var store = await stores.RequireAsync(actor.Id); var start = (from ?? DateTime.UtcNow.AddHours(7).Date.AddDays(-30)).Date; var end = (to ?? DateTime.UtcNow.AddHours(7).Date).Date;
        if (end < start || end - start > TimeSpan.FromDays(366)) throw new BusinessException("เลือกช่วงวันที่ไม่เกิน 366 วัน");
        var utcStart = start.AddHours(-7); var utcEnd = end.AddDays(1).AddHours(-7);
        var journals = await db.JournalEntries.AsNoTracking().Include(x => x.Lines).Include(x => x.Order).Where(x => (x.Order.StoreId == store.Id || (x.Order.Channel == SalesChannel.POS && x.Order.CashierUserId == actor.Id)) && x.PostedAt >= utcStart && x.PostedAt < utcEnd).OrderByDescending(x => x.PostedAt).ToListAsync();
        var expenses = await db.StoreExpenses.AsNoTracking().Where(x => x.StoreId == store.Id && x.OccurredAt >= utcStart && x.OccurredAt < utcEnd).OrderByDescending(x => x.OccurredAt).ToListAsync();
        return new StoreAccountingPage(store, start, end, journals, expenses);
    }
    public async Task<IActionResult> Accounting(DateTime? from, DateTime? to) => View(await Report(from, to));
    [HttpPost] public async Task<IActionResult> Expense(Guid key, DateTime day, decimal amount, string description, string? reference) { await stores.AddExpenseAsync(actor.Id, key, day, amount, description ?? "", reference ?? ""); return RedirectToAction(nameof(Accounting)); }
    [HttpPost] public async Task<IActionResult> ReverseExpense(long id, string reason) { await stores.ReverseExpenseAsync(actor.Id, id, reason ?? ""); return RedirectToAction(nameof(Accounting)); }
    public async Task<IActionResult> Export(DateTime? from, DateTime? to)
    {
        var r = await Report(from, to); var rows = new List<string[]> { new[] { "วันที่", "เอกสาร", "รายการ", "บัญชี", "เดบิต", "เครดิต", "ค่าใช้จ่าย" } };
        foreach (var j in r.Journals) foreach (var l in j.Lines) rows.Add(new[] { j.PostedAt.AddHours(7).ToString("yyyy-MM-dd"), j.Order.Number, j.Description, l.Name, l.Debit.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), l.Credit.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), "" });
        foreach (var e in r.Expenses) rows.Add(new[] { e.OccurredAt.AddHours(7).ToString("yyyy-MM-dd"), e.Reference, e.Description, "ค่าใช้จ่าย", "", "", e.Amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) });
        var csv = string.Join("\r\n", rows.Select(x => string.Join(",", x.Select(DashboardController.CsvCell))));
        return File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray(), "text/csv; charset=utf-8", "store-accounting.csv");
    }
}
public class MyStorePage { public MemberStore? Store { get; set; } public int Pending { get; set; } public int ReadyToShip { get; set; } public List<Order> Orders { get; set; } = []; }
public record StoreProductsPage(MemberStore Store, IReadOnlyList<AmHerb.Web.Models.CatalogItem> Products, IReadOnlyList<long> Enabled, string? Query);
public record StoreInventoryPage(MemberStore Store, List<InventoryBatch> Lots, List<InventoryTransaction> Movements, long? SkuId, int Page, bool HasNext);
public record StoreAccountingPage(MemberStore Store, DateTime From, DateTime To, List<JournalEntry> Journals, List<StoreExpense> Expenses)
{
    public decimal Sales => Journals.SelectMany(x => x.Lines).Where(x => x.Account == "4010").Sum(x => x.Credit - x.Debit);
    public decimal Cost => Journals.SelectMany(x => x.Lines).Where(x => x.Account == "5010").Sum(x => x.Debit - x.Credit);
    public decimal TokenDiscount => Journals.SelectMany(x => x.Lines).Where(x => x.Account == "4090").Sum(x => x.Debit - x.Credit);
    public decimal ExpenseTotal => Expenses.Sum(x => x.Amount);
}
