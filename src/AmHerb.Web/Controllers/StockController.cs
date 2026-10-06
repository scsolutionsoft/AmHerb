using System.Security.Claims;
using System.Text;
using AmHerb.Web.Data;
using AmHerb.Web.Domain;
using AmHerb.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AmHerb.Web.Controllers;

public record StockRow(long SkuId, long WarehouseId, string Warehouse, WarehouseKind Kind, string Owner, string Sku, string Product, int Available, int Reserved, int Expired, int Expiring, int Threshold, int OnHand, decimal Value);
public class StockPage
{
    public bool Staff { get; set; }
    public List<Warehouse> Warehouses { get; set; } = [];
    public List<StockRow> Rows { get; set; } = [];
    public List<InventoryBatch> Lots { get; set; } = [];
    public List<Member> Members { get; set; } = [];
    public WarehouseKind? Kind { get; set; }
    public long? WarehouseId { get; set; }
    public string? Q { get; set; }
    public string? Alert { get; set; }
    public long InTransit { get; set; }
    public static string Label(WarehouseKind kind) => kind switch { WarehouseKind.Company => "บริษัทกลาง", WarehouseKind.Dealer => "ดีลเลอร์ / ผู้ขาย", _ => "สมาชิก" };
}

[Authorize]
public class StockController(AmHerbDbContext db, AuditService audit, TimeProvider clock) : Controller
{
    public static bool CanSeeAll(ClaimsPrincipal user) => new[] { "SuperAdmin", "Admin", "Warehouse", "Finance", "Marketing" }.Any(user.IsInRole);
    public static IQueryable<Warehouse> Visible(AmHerbDbContext db, ClaimsPrincipal user)
    {
        var uid = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return CanSeeAll(user) ? db.Warehouses : db.Warehouses.Where(x => x.OwnerMember != null && x.OwnerMember.UserId == uid && x.OwnerMember.Status == MemberStatus.Active);
    }
    private async Task<StockPage> Report(WarehouseKind? kind, long? warehouseId, string? q, string? alert)
    {
        var warehouses = await Visible(db, User).AsNoTracking().Include(x => x.OwnerMember).OrderBy(x => x.Kind).ThenBy(x => x.Name).ToListAsync();
        var ids = warehouses.Where(x => (kind == null || x.Kind == kind) && (warehouseId == null || x.Id == warehouseId)).Select(x => x.Id).ToArray();
        var now = clock.GetUtcNow().UtcDateTime;
        var query = db.InventoryBatches.AsNoTracking().Include(x => x.Sku).ThenInclude(x => x.Product).Include(x => x.Warehouse).ThenInclude(x => x.OwnerMember).Where(x => ids.Contains(x.WarehouseId));
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(x => x.Sku.Code.Contains(q) || x.Sku.Product.Name.Contains(q) || x.LotNo.Contains(q));
        var lots = await query.OrderBy(x => x.ExpDate).ToListAsync();
        var rows = lots.GroupBy(x => new { x.WarehouseId, x.SkuId }).Select(g => {
            var b = g.First();
            return new StockRow(b.SkuId, b.WarehouseId, b.Warehouse.Name, b.Warehouse.Kind, b.Warehouse.OwnerMember?.Name ?? "AM HERB", b.Sku.Code, b.Sku.Product.Name,
                g.Where(x => x.ExpDate > now && x.Warehouse.Active).Sum(x => x.QtyAvailable), g.Sum(x => x.QtyReserved),
                g.Where(x => x.ExpDate <= now).Sum(x => x.QtyAvailable), g.Where(x => x.ExpDate > now && x.ExpDate <= now.AddDays(90)).Sum(x => x.QtyAvailable), b.Sku.LowStockThreshold, g.Sum(x => x.QtyAvailable + x.QtyReserved),
                g.Sum(x => (x.QtyAvailable + x.QtyReserved) * x.Cost));
        }).ToList();
        rows = rows.Where(x => alert switch { "low" => x.Available <= x.Threshold, "expiry" => x.Expiring > 0, "expired" => x.Expired > 0, _ => true }).ToList();
        var selected = rows.Select(x => (x.WarehouseId, x.Sku)).ToHashSet();
        var transit = db.StockTransfers.Where(x => x.Status == TransferStatus.Dispatched && (ids.Contains(x.SourceWarehouseId) || ids.Contains(x.DestinationWarehouseId)));
        if (!string.IsNullOrWhiteSpace(q)) transit = transit.Where(x => x.Sku.Code.Contains(q) || x.Sku.Product.Name.Contains(q) || x.Allocations.Any(a => a.SourceBatch.LotNo.Contains(q)));
        return new StockPage { Staff = CanSeeAll(User), Warehouses = warehouses, Rows = rows, Lots = lots.Where(x => selected.Contains((x.WarehouseId, x.Sku.Code))).ToList(), Kind = kind, WarehouseId = warehouseId, Q = q, Alert = alert, InTransit = await transit.SumAsync(x => (long?)x.Quantity) ?? 0 };
    }
    public async Task<IActionResult> Index(WarehouseKind? kind, long? warehouseId, string? q, string? alert)
    {
        var model = await Report(kind, warehouseId, q, alert);
        if (new[] { "SuperAdmin", "Admin", "Warehouse" }.Any(User.IsInRole)) model.Members = await db.Members.AsNoTracking().Where(x => x.Status == MemberStatus.Active).OrderBy(x => x.Code).ToListAsync();
        return View(model);
    }
    public async Task<IActionResult> Export(WarehouseKind? kind, long? warehouseId, string? q, string? alert)
    {
        var m = await Report(kind, warehouseId, q, alert);
        static string Cell(string s) => "\"" + ((s.Length > 0 && "=+-@\t\r\n".Contains(s[0]) ? "'" : "") + s).Replace("\"", "\"\"") + "\"";
        var header = new List<string> { "ประเภท", "คลัง", "เจ้าของ", "SKU", "สินค้า", "คงคลัง", "พร้อมขาย", "จอง", "หมดอายุ", "ใกล้หมดอายุ 90 วัน" };
        if (m.Staff) header.Add("มูลค่าทุนคงคลัง");
        var lines = new List<string> { string.Join(",", header.Select(Cell)) };
        foreach (var r in m.Rows) { var cells = new List<string> { StockPage.Label(r.Kind), r.Warehouse, r.Owner, r.Sku, r.Product, r.OnHand.ToString(), r.Available.ToString(), r.Reserved.ToString(), r.Expired.ToString(), r.Expiring.ToString() }; if (m.Staff) cells.Add(r.Value.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)); lines.Add(string.Join(",", cells.Select(Cell))); }
        return File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(string.Join("\r\n", lines))).ToArray(), "text/csv; charset=utf-8", "AMHERB-stock-" + clock.GetUtcNow().ToString("yyyyMMdd-HHmm") + ".csv");
    }
    [HttpPost, Authorize(Policy = "Inventory")]
    public async Task<IActionResult> CreateWarehouse(string name, WarehouseKind kind, long? ownerMemberId, string reason)
    {
        if (!Enum.IsDefined(kind) || string.IsNullOrWhiteSpace(name) || name.Length > 120 || string.IsNullOrWhiteSpace(reason)) throw new BusinessException("กรุณาระบุชื่อคลัง ประเภท และเหตุผลให้ครบถ้วน");
        if (kind == WarehouseKind.Company) ownerMemberId = null;
        else if (!await db.Members.AnyAsync(x => x.Id == ownerMemberId && x.Status == MemberStatus.Active)) throw new BusinessException("กรุณาเลือกเจ้าของคลังที่ใช้งานอยู่");
        var warehouse = new Warehouse { Name = name.Trim(), Kind = kind, OwnerMemberId = ownerMemberId };
        db.Warehouses.Add(warehouse); audit.Add("Warehouse.Create", name, $"{kind}; owner={ownerMemberId}; {reason}"); await db.SaveChangesAsync();
        TempData["Success"] = "สร้างคลังแล้ว สามารถรับสินค้าเข้าคลังและผูกกับจุดขาย POS ได้";
        return RedirectToAction(nameof(Index), new { warehouseId = warehouse.Id });
    }
}



