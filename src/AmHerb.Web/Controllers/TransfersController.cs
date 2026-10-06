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
public class TransfersController(AmHerbDbContext db, StockTransferService transfers) : Controller
{
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
        var transfer = await transfers.Visible(User).AsNoTracking().Include(x => x.SourceWarehouse).Include(x => x.DestinationWarehouse).ThenInclude(x => x.OwnerMember).Include(x => x.Sku).ThenInclude(x => x.Product).Include(x => x.Allocations).ThenInclude(x => x.SourceBatch).SingleOrDefaultAsync(x => x.Id == id);
        return transfer == null ? NotFound() : View(transfer);
    }
    [HttpPost] public async Task<IActionResult> Create(Guid key, long sourceId, long destinationId, long skuId, int quantity, string reason)
    { var transfer = await transfers.RequestAsync(User, key, sourceId, destinationId, skuId, quantity, reason); TempData["Success"] = "ส่งคำขอเบิกแล้ว รอผู้ดูแลอนุมัติจ่ายสินค้า"; return RedirectToAction(nameof(Detail), new { id = transfer.Id }); }
    [HttpPost, Authorize(Policy = "Inventory")] public async Task<IActionResult> Dispatch(long id, string tracking, string reason)
    { await transfers.DispatchAsync(User, id, tracking, reason); return Done(id, "จ่ายสินค้าจากคลังต้นทางแล้ว รอปลายทางยืนยันรับ"); }
    [HttpPost] public async Task<IActionResult> Receive(long id, string reason)
    { await transfers.ReceiveAsync(User, id, reason); return Done(id, "ยืนยันรับและเพิ่มสต๊อกปลายทางแล้ว"); }
    [HttpPost] public async Task<IActionResult> Cancel(long id, string reason)
    { await transfers.CancelAsync(User, id, reason); return Done(id, "ยกเลิกใบเบิกแล้ว"); }
    private IActionResult Done(long id, string message) { TempData["Success"] = message; return RedirectToAction(nameof(Detail), new { id }); }
}

