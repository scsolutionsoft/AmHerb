using System.Data;
using System.Security.Claims;
using AmHerb.Web.Data;
using AmHerb.Web.Domain;
using Microsoft.EntityFrameworkCore;

namespace AmHerb.Web.Services;
public class StockTransferService(AmHerbDbContext db, TimeProvider clock, AuditService audit, INotificationService notifications)
{
    public static bool Manager(ClaimsPrincipal user) => new[] { "SuperAdmin", "Admin", "Warehouse" }.Any(user.IsInRole);
    private static string UserId(ClaimsPrincipal user) => user.Identity?.IsAuthenticated == true ? user.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new BusinessException("กรุณาเข้าสู่ระบบ") : throw new BusinessException("กรุณาเข้าสู่ระบบ");
    public IQueryable<StockTransfer> Visible(ClaimsPrincipal user)
    {
        var uid = UserId(user);
        return new[] { "SuperAdmin", "Admin", "Warehouse", "Finance", "Marketing" }.Any(user.IsInRole) ? db.StockTransfers : db.StockTransfers.Where(x =>
            (x.DestinationWarehouse.OwnerMember!.UserId == uid && x.DestinationWarehouse.OwnerMember.Status == MemberStatus.Active) ||
            (x.SourceWarehouse.OwnerMember!.UserId == uid && x.SourceWarehouse.OwnerMember.Status == MemberStatus.Active));
    }
    private static bool Owns(Warehouse w, string uid) => w.OwnerMember?.UserId == uid && w.OwnerMember.Status == MemberStatus.Active;
    private static void Note(string reason) { if (string.IsNullOrWhiteSpace(reason) || reason.Length > 900) throw new BusinessException("กรุณาระบุเหตุผล ไม่เกิน 900 ตัวอักษร"); }
    private async Task<StockTransfer> Load(long id) => await db.StockTransfers.Include(x => x.SourceWarehouse).ThenInclude(x => x.OwnerMember).Include(x => x.DestinationWarehouse).ThenInclude(x => x.OwnerMember).Include(x => x.Allocations).ThenInclude(x => x.SourceBatch).SingleOrDefaultAsync(x => x.Id == id) ?? throw new BusinessException("ไม่พบใบเบิกสินค้า");
    public async Task<StockTransfer> RequestAsync(ClaimsPrincipal user, Guid key, long sourceId, long destinationId, long skuId, int quantity, string reason)
    {
        var uid = UserId(user); Note(reason);
        if (key == Guid.Empty || quantity <= 0 || quantity > 1000000 || sourceId == destinationId) throw new BusinessException("ข้อมูลใบเบิกไม่ถูกต้อง");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var previous = await db.StockTransfers.SingleOrDefaultAsync(x => x.RequestKey == key);
        if (previous != null)
        {
            if (previous.RequestedBy != uid || previous.SourceWarehouseId != sourceId || previous.DestinationWarehouseId != destinationId || previous.SkuId != skuId || previous.Quantity != quantity || previous.Reason != reason) throw new BusinessException("เลขอ้างอิงซ้ำกับข้อมูลใบเบิกอื่น");
            return previous;
        }
        var source = await db.Warehouses.Include(x => x.OwnerMember).SingleOrDefaultAsync(x => x.Id == sourceId && x.Active);
        var destination = await db.Warehouses.Include(x => x.OwnerMember).SingleOrDefaultAsync(x => x.Id == destinationId && x.Active);
        if (source == null || destination == null || (source.OwnerMember != null && source.OwnerMember.Status != MemberStatus.Active) || (destination.OwnerMember != null && destination.OwnerMember.Status != MemberStatus.Active)) throw new BusinessException("คลังหรือเจ้าของคลังไม่พร้อมใช้งาน");
        if (!Manager(user) && (source.Kind != WarehouseKind.Company || !Owns(destination, uid))) throw new BusinessException("ขอเบิกได้จากคลังบริษัทเข้าคลังของตนเองเท่านั้น");
        if (!await db.Skus.AnyAsync(x => x.Id == skuId && x.Active && x.Product.Active)) throw new BusinessException("สินค้าไม่พร้อมใช้งาน");
        var transfer = new StockTransfer { RequestKey = key, SourceWarehouseId = sourceId, DestinationWarehouseId = destinationId, SkuId = skuId, Quantity = quantity, RequestedBy = uid, Reason = reason, CreatedAt = clock.GetUtcNow().UtcDateTime };
        db.StockTransfers.Add(transfer); await db.SaveChangesAsync();
        audit.Add("Transfer.Request", transfer.Id, reason);
        await notifications.AddAsync(null, $"transfer:{transfer.Id}:request", $"ใบเบิก #{transfer.Id} รออนุมัติจ่ายสินค้า {quantity} ชิ้น");
        await db.SaveChangesAsync(); await tx.CommitAsync(); return transfer;
    }
    public async Task DispatchAsync(ClaimsPrincipal user, long id, string tracking, string reason)
    {
        var uid = UserId(user); Note(reason);
        if (!Manager(user)) throw new BusinessException("เฉพาะผู้ดูแลคลังอนุมัติจ่ายสินค้าได้");
        if (string.IsNullOrWhiteSpace(tracking) || tracking.Length > 200) throw new BusinessException("กรุณาระบุเลขส่งของหรือเลขอ้างอิงการส่งมอบ");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var t = await Load(id);
        if (t.Status is TransferStatus.Dispatched or TransferStatus.Received) return;
        if (t.Status != TransferStatus.Requested) throw new BusinessException("ใบเบิกนี้ไม่อยู่ในสถานะรอจ่าย");
        if (!t.SourceWarehouse.Active || !t.DestinationWarehouse.Active || (t.DestinationWarehouse.OwnerMember != null && t.DestinationWarehouse.OwnerMember.Status != MemberStatus.Active) || (t.SourceWarehouse.OwnerMember != null && t.SourceWarehouse.OwnerMember.Status != MemberStatus.Active)) throw new BusinessException("คลังหรือเจ้าของคลังไม่พร้อมใช้งาน");
        var now = clock.GetUtcNow().UtcDateTime;
        var batches = await db.InventoryBatches.Where(x => x.WarehouseId == t.SourceWarehouseId && x.SkuId == t.SkuId && x.ExpDate > now && x.QtyAvailable > 0).OrderBy(x => x.ExpDate).ThenBy(x => x.Id).ToListAsync();
        if (batches.Sum(x => (long)x.QtyAvailable) < t.Quantity) throw new BusinessException("สต๊อกพร้อมจ่ายไม่เพียงพอ กรุณารับสินค้าเข้าคลังต้นทาง");
        var remaining = t.Quantity;
        foreach (var batch in batches)
        {
            var take = Math.Min(remaining, batch.QtyAvailable); if (take == 0) break;
            batch.QtyAvailable -= take;
            t.Allocations.Add(new TransferAllocation { SourceBatchId = batch.Id, Quantity = take });
            db.InventoryTransactions.Add(new InventoryTransaction { InventoryBatchId = batch.Id, Kind = InventoryKind.TransferOut, Quantity = -take, PostedAt = now, Reason = $"Transfer #{t.Id}; {reason}" });
            remaining -= take;
        }
        t.Status = TransferStatus.Dispatched; t.DispatchedBy = uid; t.DispatchedAt = now; t.Tracking = tracking.Trim();
        audit.Add("Transfer.Dispatch", t.Id, reason);
        await notifications.AddAsync(t.DestinationWarehouse.OwnerMemberId, $"transfer:{t.Id}:dispatch", $"ใบเบิก #{t.Id} ส่งแล้ว {t.Quantity} ชิ้น อ้างอิง {t.Tracking} กรุณาตรวจและยืนยันรับสินค้า");
        await db.SaveChangesAsync(); await tx.CommitAsync();
    }
    public async Task ReceiveAsync(ClaimsPrincipal user, long id, string reason)
    {
        var uid = UserId(user); Note(reason);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var t = await Load(id);
        if (!Manager(user) && !Owns(t.DestinationWarehouse, uid)) throw new BusinessException("เฉพาะเจ้าของคลังปลายทางหรือผู้ดูแลคลังยืนยันรับสินค้าได้");
        if (t.Status == TransferStatus.Received) return;
        if (t.Status != TransferStatus.Dispatched) throw new BusinessException("ยืนยันรับได้เฉพาะใบเบิกที่ส่งสินค้าแล้ว");
        if (!t.DestinationWarehouse.Active) throw new BusinessException("คลังปลายทางปิดใช้งาน กรุณาติดต่อผู้ดูแล");
        foreach (var allocation in t.Allocations)
        {
            var source = allocation.SourceBatch;
            var destination = await db.InventoryBatches.SingleOrDefaultAsync(x => x.WarehouseId == t.DestinationWarehouseId && x.SkuId == t.SkuId && x.LotNo == source.LotNo);
            if (destination == null) { destination = new InventoryBatch { WarehouseId = t.DestinationWarehouseId, SkuId = t.SkuId, LotNo = source.LotNo, MfgDate = source.MfgDate, ExpDate = source.ExpDate, Cost = source.Cost }; db.InventoryBatches.Add(destination); }
            else if (destination.Cost != source.Cost || destination.MfgDate != source.MfgDate || destination.ExpDate != source.ExpDate) throw new BusinessException("ข้อมูลล็อตปลายทางไม่ตรงต้นทาง กรุณาให้ผู้ดูแลตรวจสอบก่อนรับเข้า");
            destination.QtyReceived += allocation.Quantity; destination.QtyAvailable += allocation.Quantity;
            db.InventoryTransactions.Add(new InventoryTransaction { InventoryBatch = destination, Kind = InventoryKind.TransferIn, Quantity = allocation.Quantity, PostedAt = clock.GetUtcNow().UtcDateTime, Reason = $"Transfer #{t.Id}; {reason}" });
        }
        t.Status = TransferStatus.Received; t.ReceivedBy = uid; t.ReceivedAt = clock.GetUtcNow().UtcDateTime;
        audit.Add("Transfer.Receive", t.Id, reason);
        await notifications.AddAsync(t.SourceWarehouse.OwnerMemberId, $"transfer:{t.Id}:receive", $"ใบเบิก #{t.Id} ปลายทางรับสินค้าแล้ว {t.Quantity} ชิ้น");
        await db.SaveChangesAsync(); await tx.CommitAsync();
    }
    public async Task CancelAsync(ClaimsPrincipal user, long id, string reason)
    {
        var uid = UserId(user); Note(reason);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var t = await Load(id);
        if (!Manager(user) && !(t.RequestedBy == uid && Owns(t.DestinationWarehouse, uid))) throw new BusinessException("ไม่มีสิทธิ์ยกเลิกใบเบิกนี้");
        if (t.Status == TransferStatus.Cancelled) return;
        if (t.Status != TransferStatus.Requested) throw new BusinessException("ยกเลิกได้ก่อนจ่ายสินค้าเท่านั้น");
        t.Status = TransferStatus.Cancelled; audit.Add("Transfer.Cancel", t.Id, reason);
        await notifications.AddAsync(t.DestinationWarehouse.OwnerMemberId, $"transfer:{t.Id}:cancel", $"ใบเบิก #{t.Id} ยกเลิก: {reason}");
        await db.SaveChangesAsync(); await tx.CommitAsync();
    }
}

