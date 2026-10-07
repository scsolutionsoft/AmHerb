using System.Security.Claims;
using AmHerb.Web.Data;
using AmHerb.Web.Domain;
using AmHerb.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AmHerb.Web.Controllers;
public class TransferPage
{
    public List<StockTransfer> Transfers { get; set; } = [];
    public List<Warehouse> Sources { get; set; } = [];
    public List<Warehouse> Destinations { get; set; } = [];
    public List<Sku> Skus { get; set; } = [];
    public TransferStatus? Status { get; set; }
    public bool Manager { get; set; }
    public int Page { get; set; }
    public bool HasNext { get; set; }
    public static string Label(TransferStatus status) => status switch { TransferStatus.Requested => "รออนุมัติจ่าย", TransferStatus.Dispatched => "ระหว่างขนส่ง", TransferStatus.Received => "รับเข้าคลังแล้ว", _ => "ยกเลิก" };
}
[Authorize]
public class TransfersController(AmHerbDbContext db, StockTransferService transfers, IAuthorizationService authorization) : Controller
{
    [HttpGet, ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Preview(long? transferId, long sourceId, long destinationId, long skuId)
    {
        if (transferId != null)
        {
            var transfer = await transfers.Visible(User).AsNoTracking().SingleOrDefaultAsync(x => x.Id == transferId);
            if (transfer == null) return NotFound();
            sourceId = transfer.SourceWarehouseId; destinationId = transfer.DestinationWarehouseId; skuId = transfer.SkuId;
        }
        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var warehouses = await db.Warehouses.AsNoTracking().Include(x => x.OwnerMember).Where(x => x.Id == sourceId || x.Id == destinationId).ToListAsync();
        var source = warehouses.SingleOrDefault(x => x.Id == sourceId); var destination = warehouses.SingleOrDefault(x => x.Id == destinationId);
        if (source == null || destination == null || sourceId == destinationId) return BadRequest(new { message = "เลือกคลังต้นทางและปลายทางที่แตกต่างกัน" });
        if (transferId == null && !StockTransferService.Manager(User) && (source.Kind != WarehouseKind.Company || destination.OwnerMember?.UserId != uid || destination.OwnerMember?.Status != MemberStatus.Active)) return NotFound();
        var sku = await db.Skus.AsNoTracking().Include(x => x.Product).SingleOrDefaultAsync(x => x.Id == skuId); if (sku == null) return NotFound();
        var now = DateTime.UtcNow;
        var lots = await db.InventoryBatches.AsNoTracking().Where(x => x.SkuId == skuId && (x.WarehouseId == sourceId || x.WarehouseId == destinationId)).OrderBy(x => x.ExpDate).ThenBy(x => x.Id)
            .Select(x => new { x.Id, x.WarehouseId, x.LotNo, x.QtyAvailable, x.QtyReserved, x.ExpDate }).ToListAsync();
        async Task<object> Balance(Warehouse warehouse)
        {
            var rows = lots.Where(x => x.WarehouseId == warehouse.Id).ToList();
            var active = warehouse.Active && (warehouse.OwnerMember == null || warehouse.OwnerMember.Status == MemberStatus.Active) && sku.Active && sku.Product.Active;
            return new { warehouse.Id, warehouse.Name, Active = active,
                OnHand = rows.Sum(x => (long)x.QtyAvailable + x.QtyReserved), Reserved = rows.Sum(x => (long)x.QtyReserved),
                Available = active ? rows.Where(x => x.ExpDate > now).Sum(x => (long)x.QtyAvailable) : 0,
                Unavailable = rows.Where(x => x.ExpDate <= now || !active).Sum(x => (long)x.QtyAvailable),
                Incoming = await db.StockTransfers.Where(x => x.DestinationWarehouseId == warehouse.Id && x.SkuId == skuId && x.Status == TransferStatus.Dispatched).SumAsync(x => (long?)x.Quantity) ?? 0,
                Requested = await db.StockTransfers.Where(x => x.SourceWarehouseId == warehouse.Id && x.SkuId == skuId && x.Status == TransferStatus.Requested).SumAsync(x => (long?)x.Quantity) ?? 0,
                Lots = rows.Take(100).Select(x => new { x.LotNo, Available = x.ExpDate > now && active ? x.QtyAvailable : 0, Reserved = x.QtyReserved, Unavailable = x.ExpDate <= now || !active ? x.QtyAvailable : 0, Expiry = x.ExpDate.ToString("dd/MM/yyyy") }), TotalLots = rows.Count };
        }
        var priceRows = await db.ProductPrices.AsNoTracking().Where(x => x.SkuId == skuId && (x.Channel == null || x.Channel == SalesChannel.Store) && x.EffectiveFrom <= now && (x.EffectiveTo == null || x.EffectiveTo > now))
            .OrderByDescending(x => x.Channel.HasValue).ThenByDescending(x => x.EffectiveFrom).Select(x => new { x.Tier, x.Amount, x.PackQuantity }).ToListAsync();
        var prices = priceRows.GroupBy(x => x.Tier).Select(g => { var p = g.First(); return new { Tier = g.Key, p.Amount, p.PackQuantity, UnitPrice = p.PackQuantity < 1 ? 0 : Money.Round(p.Amount / p.PackQuantity) }; }).ToList();
        return Json(new { Product = sku.Product.Name, sku.Code, Description = sku.Product.Description, Image = "/Media/Thumb/" + sku.Id, Source = await Balance(source), Destination = await Balance(destination), Prices = prices, CheckedAt = Bangkok.Format(now) });
    }
    public async Task<IActionResult> Index(TransferStatus? status, int page = 1)
    {
        page = Math.Clamp(page, 1, 100000); var manager = StockTransferService.Manager(User); var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var query = transfers.Visible(User).AsNoTracking().Include(x => x.SourceWarehouse).Include(x => x.DestinationWarehouse).Include(x => x.Sku).ThenInclude(x => x.Product).Where(x => status == null || x.Status == status);
        var rows = await query.OrderByDescending(x => x.Id).Skip((page - 1) * 30).Take(31).ToListAsync();
        return View(new TransferPage { Manager = manager, Status = status, Page = page, HasNext = rows.Count > 30, Transfers = rows.Take(30).ToList(),
            Sources = await db.Warehouses.AsNoTracking().Where(x => x.Active && (manager || x.Kind == WarehouseKind.Company)).OrderBy(x => x.Name).ToListAsync(),
            Destinations = await db.Warehouses.AsNoTracking().Where(x => x.Active && (manager || (x.OwnerMember!.UserId == uid && x.OwnerMember.Status == MemberStatus.Active))).OrderBy(x => x.Name).ToListAsync(),
            Skus = await db.Skus.AsNoTracking().Include(x => x.Product).Where(x => x.Active && x.Product.Active).OrderBy(x => x.Code).ToListAsync() });
    }
    public async Task<IActionResult> Detail(long id)
    {
        var transfer = await transfers.Visible(User).AsNoTracking().Include(x => x.SourceWarehouse).Include(x => x.DestinationWarehouse).ThenInclude(x => x.OwnerMember).Include(x => x.Sku).ThenInclude(x => x.Product).Include(x => x.Allocations).ThenInclude(x => x.SourceBatch).Include(x => x.Payments).SingleOrDefaultAsync(x => x.Id == id);
        if (transfer == null) return NotFound(); ViewBag.Finance = (await authorization.AuthorizeAsync(User, "Payments")).Succeeded; return View(transfer);
    }
    [HttpPost] public async Task<IActionResult> Create(Guid key, long sourceId, long destinationId, long skuId, int quantity, string reason, TransferPaymentMethod? paymentMethod, string priceTier)
    { if (paymentMethod == null) throw new BusinessException("เลือกวิธีชำระเงิน"); var transfer = await transfers.RequestAsync(User, key, sourceId, destinationId, skuId, quantity, reason, paymentMethod, priceTier); TempData["Success"] = $"ออก {transfer.PoNumber} แล้ว กรุณาดำเนินการชำระหรือรอฝ่ายการเงินอนุมัติก่อนจ่ายสินค้า"; return RedirectToAction(nameof(Detail), new { id = transfer.Id }); }
    [HttpPost, Authorize(Policy = "Inventory")] public async Task<IActionResult> Dispatch(long id, string tracking, string reason)
    { await transfers.DispatchAsync(User, id, tracking, reason); return Done(id, "จ่ายสินค้าจากคลังต้นทางแล้ว รอปลายทางยืนยันรับ"); }
    [HttpPost] public async Task<IActionResult> Receive(long id, string reason)
    { await transfers.ReceiveAsync(User, id, reason); return Done(id, "ยืนยันรับและเพิ่มสต๊อกปลายทางแล้ว"); }
    [HttpPost] public async Task<IActionResult> Cancel(long id, string reason)
    { await transfers.CancelAsync(User, id, reason); return Done(id, "ยกเลิกใบเบิกแล้ว"); }
    private IActionResult Done(long id, string message) { TempData["Success"] = message; return RedirectToAction(nameof(Detail), new { id }); }
    [HttpPost, RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> SubmitPayment(long id, Guid key, decimal amount, DateTime paidAt, string reference, string note, IFormFile? slip)
    { await transfers.SubmitPaymentAsync(User, id, key, amount, DateTime.SpecifyKind(paidAt, DateTimeKind.Unspecified).AddHours(-7), reference, note, slip == null ? null : await MediaService.Read(slip)); return Done(id, "ส่งหลักฐานชำระแล้ว รอฝ่ายการเงินตรวจสอบ"); }
    [HttpPost, Authorize(Policy = "Payments")]
    public async Task<IActionResult> ReviewPayment(long paymentId, bool approve, string reason)
    { var id = await db.TransferPayments.Where(x => x.Id == paymentId).Select(x => (long?)x.StockTransferId).SingleOrDefaultAsync(); if (id == null) return NotFound(); await transfers.ReviewPaymentAsync(User, paymentId, approve, reason); return Done(id.Value, approve ? "ยืนยันรับเงินและอัปเดต PO แล้ว" : "ปฏิเสธหลักฐานชำระแล้ว"); }
    [HttpPost, Authorize(Policy = "Payments")]
    public async Task<IActionResult> ApproveFinancial(long id, string reference, DateTime? dueAt, string reason)
    { await transfers.ApproveFinancialAsync(User, id, reference, dueAt == null ? null : DateTime.SpecifyKind(dueAt.Value, DateTimeKind.Unspecified).AddHours(-7), reason); return Done(id, "ฝ่ายการเงินอนุมัติยอดเรียกเก็บแล้ว"); }
    public async Task<IActionResult> PaymentSlip(long id)
    { var p = await db.TransferPayments.AsNoTracking().Include(x => x.StockTransfer).SingleOrDefaultAsync(x => x.Id == id); if (p == null || p.Slip == null || !await transfers.Visible(User).AnyAsync(x => x.Id == p.StockTransferId)) return NotFound(); Response.Headers.CacheControl = "private, no-store"; return File(p.Slip, p.SlipType); }
    public async Task<IActionResult> Document(long id, string type = "po")
    {
        var t = await transfers.Visible(User).AsNoTracking().Include(x => x.SourceWarehouse).ThenInclude(x => x.OwnerMember).Include(x => x.DestinationWarehouse).ThenInclude(x => x.OwnerMember).Include(x => x.Sku).ThenInclude(x => x.Product).Include(x => x.Allocations).ThenInclude(x => x.SourceBatch).Include(x => x.Payments).SingleOrDefaultAsync(x => x.Id == id);
        if (t == null || type is not ("po" or "dispatch" or "receipt") || type == "dispatch" && t.DispatchedAt == null || type == "receipt" && t.ReceivedAt == null) return NotFound();
        ViewBag.DocumentType = type; Response.Headers.CacheControl = "private, no-store"; return View(t);
    }
}
