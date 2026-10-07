using System.Data;
using AmHerb.Web.Data;
using AmHerb.Web.Domain;
using Microsoft.EntityFrameworkCore;

namespace AmHerb.Web.Services;

public class InventoryService(AmHerbDbContext db, TimeProvider clock, AuditService audit)
{
    public async Task ReceiveAsync(long skuId, long warehouseId, string lot, DateTime mfg, DateTime expiry, int quantity, decimal cost, string reason)
    {
        if (quantity <= 0 || cost < 0 || expiry <= clock.GetUtcNow().UtcDateTime || mfg >= expiry || string.IsNullOrWhiteSpace(lot)) throw new BusinessException("ข้อมูลรับสินค้า/ล็อต/วันหมดอายุไม่ถูกต้อง");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        if (!await db.Warehouses.AnyAsync(x => x.Id == warehouseId && x.Active)) throw new BusinessException("คลังไม่พร้อมใช้งาน");
        var batch = await db.InventoryBatches.SingleOrDefaultAsync(x => x.WarehouseId == warehouseId && x.SkuId == skuId && x.LotNo == lot);
        if (batch == null) { batch = new InventoryBatch { SkuId = skuId, WarehouseId = warehouseId, LotNo = lot, MfgDate = mfg, ExpDate = expiry, Cost = cost }; db.InventoryBatches.Add(batch); }
        else if (batch.ExpDate != expiry || batch.MfgDate != mfg || batch.Cost != cost) throw new BusinessException("ข้อมูลล็อตเดิมไม่ตรงกัน");
        batch.QtyReceived += quantity; batch.QtyAvailable += quantity;
        db.InventoryTransactions.Add(new InventoryTransaction { InventoryBatch = batch, Kind = InventoryKind.Receive, Quantity = quantity, Reason = reason });
        audit.Add("Inventory.Receive", skuId, reason);
        await db.SaveChangesAsync(); await tx.CommitAsync();
    }
    public async Task ReserveAsync(OrderItem item, long warehouseId)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var batches = await db.InventoryBatches.Where(x => x.SkuId == item.SkuId && x.WarehouseId == warehouseId && x.Warehouse.Active && x.ExpDate > now && x.QtyAvailable > 0).OrderBy(x => x.ExpDate).ThenBy(x => x.Id).ToListAsync();
        var needed = item.Quantity;
        foreach (var batch in batches)
        {
            var take = Math.Min(batch.QtyAvailable, needed); if (take == 0) continue;
            batch.QtyAvailable -= take; batch.QtyReserved += take;
            item.Allocations.Add(new StockAllocation { InventoryBatchId = batch.Id, Quantity = take });
            db.InventoryTransactions.Add(new InventoryTransaction { InventoryBatchId = batch.Id, OrderId = item.OrderId, Kind = InventoryKind.Reserve, Quantity = -take });
            needed -= take; if (needed == 0) break;
        }
        if (needed != 0) throw new BusinessException($"สต็อก {item.Name} ไม่เพียงพอ");
    }
    public async Task FinalizeAsync(Order order, bool sale)
    {
        foreach (var item in order.Items)
        foreach (var a in await db.StockAllocations.Include(x => x.InventoryBatch).Where(x => x.OrderItemId == item.Id).ToListAsync())
        {
            a.InventoryBatch.QtyReserved -= a.Quantity;
            if (!sale) a.InventoryBatch.QtyAvailable += a.Quantity;
            db.InventoryTransactions.Add(new InventoryTransaction { InventoryBatchId = a.InventoryBatchId, OrderId = order.Id, Kind = sale ? InventoryKind.Sale : InventoryKind.Release, Quantity = sale ? -a.Quantity : a.Quantity });
        }
    }
    public async Task<decimal> ReturnAsync(OrderItem item, int quantity, bool sellable, string reason)
    {
        decimal restoredCost = 0;
        foreach (var a in await db.StockAllocations.Include(x => x.InventoryBatch).Where(x => x.OrderItemId == item.Id).OrderBy(x => x.Id).ToListAsync())
        {
            var amount = Math.Min(quantity, a.Quantity - a.ReturnedQuantity); if (amount <= 0) continue;
            a.ReturnedQuantity += amount;
            var restock = sellable && a.InventoryBatch.ExpDate > clock.GetUtcNow().UtcDateTime;
            if (restock) { a.InventoryBatch.QtyAvailable += amount; restoredCost += amount * a.InventoryBatch.Cost; }
            db.InventoryTransactions.Add(new InventoryTransaction { InventoryBatchId = a.InventoryBatchId, OrderId = item.OrderId, Kind = restock ? InventoryKind.Return : InventoryKind.Damage, Quantity = restock ? amount : 0, Reason = $"Returned {amount}; {reason}" });
            quantity -= amount; if (quantity == 0) break;
        }
        return restoredCost;
    }
    public async Task AdjustAsync(long batchId, int delta, InventoryKind kind, string reason, string? expectedVersion = null)
    {
        if (kind is not (InventoryKind.Adjust or InventoryKind.Damage or InventoryKind.Expire) || delta == 0 || (kind != InventoryKind.Adjust && delta > 0)) throw new BusinessException("ประเภทปรับสต็อกไม่ถูกต้อง");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var batch = await db.InventoryBatches.SingleAsync(x => x.Id == batchId);
        if (string.IsNullOrWhiteSpace(reason) || reason.Length > 1000) throw new BusinessException("ระบุเหตุผลปรับสต๊อกไม่เกิน 1,000 ตัวอักษร");
        if (expectedVersion != null && expectedVersion != Convert.ToBase64String(batch.RowVersion)) throw new BusinessException("ยอดสต๊อกเปลี่ยนแล้ว กรุณาโหลดข้อมูลล่าสุดและตรวจจำนวนอีกครั้ง");
        if ((long)batch.QtyAvailable + delta < 0 || (long)batch.QtyAvailable + delta > int.MaxValue) throw new BusinessException("จำนวนหลังปรับไม่ถูกต้อง หรือปรับลดเกินยอดที่ยังไม่จอง");
        batch.QtyAvailable += delta;
        db.InventoryTransactions.Add(new InventoryTransaction { InventoryBatchId = batchId, Kind = kind, Quantity = delta, Reason = reason });
        audit.Add("Inventory.Adjust", batchId, reason); await db.SaveChangesAsync(); await tx.CommitAsync();
    }
}
