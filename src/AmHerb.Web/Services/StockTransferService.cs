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
    public async Task<StockTransfer> RequestAsync(ClaimsPrincipal user, Guid key, long sourceId, long destinationId, long skuId, int quantity, string reason, TransferPaymentMethod? paymentMethod = null, string priceTier = "Wholesale")
    {
        var uid = UserId(user); Note(reason);
        if (key == Guid.Empty || quantity <= 0 || quantity > 1000000 || sourceId == destinationId || (paymentMethod != null && !Enum.IsDefined(paymentMethod.Value))) throw new BusinessException("ข้อมูลใบเบิกไม่ถูกต้อง");
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
        var selectedMethod = paymentMethod ?? TransferPaymentMethod.NoCharge;
        if (paymentMethod != null && selectedMethod == TransferPaymentMethod.NoCharge && !Manager(user)) throw new BusinessException("เฉพาะผู้ดูแลกำหนดรายการไม่เรียกเก็บได้");
        if (selectedMethod == TransferPaymentMethod.MemberCredit && destination.OwnerMemberId == null) throw new BusinessException("เครดิตสมาชิกใช้ได้กับคลังสมาชิกหรือดีลเลอร์เท่านั้น");
        decimal unitPrice = 0;
        if (selectedMethod != TransferPaymentMethod.NoCharge)
        {
            if (priceTier is not ("Retail" or "Wholesale" or "Promo" or "Pack6" or "Pack12")) throw new BusinessException("ประเภทราคาไม่ถูกต้อง");
            var now = clock.GetUtcNow().UtcDateTime;
            var price = await db.ProductPrices.Where(x => x.SkuId == skuId && x.Tier == priceTier && (x.Channel == null || x.Channel == SalesChannel.Store) && x.EffectiveFrom <= now && (x.EffectiveTo == null || x.EffectiveTo > now))
                .OrderByDescending(x => x.Channel.HasValue).ThenByDescending(x => x.EffectiveFrom).Select(x => new { x.Amount, x.PackQuantity }).FirstOrDefaultAsync()
                ?? throw new BusinessException("ไม่พบราคาที่มีผลสำหรับประเภทที่เลือก");
            if (price.PackQuantity < 1 || Money.Round(price.Amount / price.PackQuantity) * price.PackQuantity != price.Amount) throw new BusinessException("ราคาแพ็กต้องหารเป็นราคาต่อชิ้นได้ถึงสองตำแหน่ง กรุณาติดต่อผู้ดูแล");
            unitPrice = price.Amount / price.PackQuantity;
        }
        var charge = Money.Round(unitPrice * quantity);
        var financial = selectedMethod switch { TransferPaymentMethod.NoCharge => TransferFinancialStatus.Waived, TransferPaymentMethod.MemberCredit => TransferFinancialStatus.CreditReview, _ => TransferFinancialStatus.AwaitingPayment };
        var transfer = new StockTransfer { RequestKey = key, PoNumber = "PO-" + clock.GetUtcNow().ToString("yyyyMMdd") + "-" + key.ToString("N")[..8].ToUpperInvariant(), SourceWarehouseId = sourceId, DestinationWarehouseId = destinationId, SkuId = skuId, Quantity = quantity, RequestedBy = uid, Reason = reason, CreatedAt = clock.GetUtcNow().UtcDateTime,
            PaymentMethod = selectedMethod, FinancialStatus = financial, PriceTier = selectedMethod == TransferPaymentMethod.NoCharge ? "NoCharge" : priceTier, UnitPrice = unitPrice, ChargeAmount = charge };
        db.StockTransfers.Add(transfer); await db.SaveChangesAsync();
        audit.Add("Transfer.Request", transfer.Id, reason);
        await notifications.AddAsync(null, $"transfer:{transfer.Id}:request", $"{transfer.PoNumber} รอฝ่ายคลัง/การเงินตรวจสอบ {charge:N2} บาท", "/Transfers/Detail/" + transfer.Id);
        if (destination.OwnerMemberId != null) await notifications.AddAsync(destination.OwnerMemberId, $"transfer:{transfer.Id}:po", $"ออก {transfer.PoNumber} จำนวน {quantity} ชิ้น ยอด {charge:N2} บาท", "/Transfers/Detail/" + transfer.Id);
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
        if (t.FinancialStatus is not (TransferFinancialStatus.Paid or TransferFinancialStatus.Approved or TransferFinancialStatus.Waived)) throw new BusinessException("ฝ่ายการเงินยังไม่อนุมัติยอดเรียกเก็บของ PO นี้");
        if (!t.SourceWarehouse.Active || !t.DestinationWarehouse.Active || (t.DestinationWarehouse.OwnerMember != null && t.DestinationWarehouse.OwnerMember.Status != MemberStatus.Active) || (t.SourceWarehouse.OwnerMember != null && t.SourceWarehouse.OwnerMember.Status != MemberStatus.Active)) throw new BusinessException("คลังหรือเจ้าของคลังไม่พร้อมใช้งาน");
        var now = clock.GetUtcNow().UtcDateTime;
        if (!await db.Skus.AnyAsync(x => x.Id == t.SkuId && x.Active && x.Product.Active)) throw new BusinessException("สินค้าปิดใช้งาน ไม่สามารถจ่ายโอนได้");
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
        await notifications.AddAsync(t.DestinationWarehouse.OwnerMemberId, $"transfer:{t.Id}:dispatch", $"{t.PoNumber} ส่งแล้ว {t.Quantity} ชิ้น อ้างอิง {t.Tracking} กรุณาตรวจใบส่งของและยืนยันรับ", "/Transfers/Detail/" + t.Id);
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
        if (!t.DestinationWarehouse.Active || (t.DestinationWarehouse.OwnerMember != null && t.DestinationWarehouse.OwnerMember.Status != MemberStatus.Active)) throw new BusinessException("คลังปลายทางหรือเจ้าของคลังไม่พร้อมใช้งาน กรุณาติดต่อผู้ดูแล");
        foreach (var allocation in t.Allocations)
        {
            var source = allocation.SourceBatch;
            var destination = await db.InventoryBatches.SingleOrDefaultAsync(x => x.WarehouseId == t.DestinationWarehouseId && x.SkuId == t.SkuId && x.LotNo == source.LotNo);
            if (destination == null) { destination = new InventoryBatch { WarehouseId = t.DestinationWarehouseId, SkuId = t.SkuId, LotNo = source.LotNo, MfgDate = source.MfgDate, ExpDate = source.ExpDate, Cost = source.Cost }; db.InventoryBatches.Add(destination); }
            else if (destination.Cost != source.Cost || destination.MfgDate != source.MfgDate || destination.ExpDate != source.ExpDate) throw new BusinessException("ข้อมูลล็อตปลายทางไม่ตรงต้นทาง กรุณาให้ผู้ดูแลตรวจสอบก่อนรับเข้า");
            if ((long)destination.QtyReceived + allocation.Quantity > int.MaxValue || (long)destination.QtyAvailable + allocation.Quantity > int.MaxValue) throw new BusinessException("จำนวนสินค้าปลายทางเกินขีดจำกัด กรุณาติดต่อผู้ดูแล");
            destination.QtyReceived += allocation.Quantity; destination.QtyAvailable += allocation.Quantity;
            db.InventoryTransactions.Add(new InventoryTransaction { InventoryBatch = destination, Kind = InventoryKind.TransferIn, Quantity = allocation.Quantity, PostedAt = clock.GetUtcNow().UtcDateTime, Reason = $"Transfer #{t.Id}; {reason}" });
        }
        t.Status = TransferStatus.Received; t.ReceivedBy = uid; t.ReceivedAt = clock.GetUtcNow().UtcDateTime;
        audit.Add("Transfer.Receive", t.Id, reason);
        await notifications.AddAsync(t.SourceWarehouse.OwnerMemberId, $"transfer:{t.Id}:receive", $"{t.PoNumber} ปลายทางรับสินค้าแล้ว {t.Quantity} ชิ้น", "/Transfers/Detail/" + t.Id);
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
        if (t.PaidAmount > 0 || t.FinancialStatus == TransferFinancialStatus.Approved) throw new BusinessException("รายการนี้รับเงินหรืออนุมัติเครดิตแล้ว กรุณาให้ฝ่ายการเงินยกเลิกหรือคืนยอดก่อน");
        t.Status = TransferStatus.Cancelled; audit.Add("Transfer.Cancel", t.Id, reason);
        t.FinancialStatus = TransferFinancialStatus.Cancelled;
        await notifications.AddAsync(t.DestinationWarehouse.OwnerMemberId, $"transfer:{t.Id}:cancel", $"ใบเบิก #{t.Id} ยกเลิก: {reason}");
        await db.SaveChangesAsync(); await tx.CommitAsync();
    }

    public async Task<long> SubmitPaymentAsync(ClaimsPrincipal user, long id, Guid key, decimal amount, DateTime paidAt, string reference, string note, byte[]? slip)
    {
        var uid = UserId(user); Note(note); if (key == Guid.Empty || amount <= 0 || Money.Round(amount) != amount || string.IsNullOrWhiteSpace(reference) || reference.Length > 200) throw new BusinessException("ตรวจยอด วันที่ และเลขอ้างอิงชำระเงิน");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable); var t = await Load(id);
        if (!Manager(user) && !Owns(t.DestinationWarehouse, uid)) throw new BusinessException("เฉพาะคลังปลายทางหรือผู้ดูแลแจ้งชำระได้");
        if (t.PaymentMethod is not (TransferPaymentMethod.BankTransfer or TransferPaymentMethod.MemberCredit) || t.Status == TransferStatus.Cancelled || t.PaidAmount + amount > t.ChargeAmount) throw new BusinessException("ยอดหรือวิธีชำระไม่ตรงกับ PO");
        var previous = await db.TransferPayments.SingleOrDefaultAsync(x => x.RequestKey == key); if (previous != null) return previous.Id;
        if (slip == null) throw new BusinessException("กรุณาแนบสลิปโอนเงิน"); var type = MediaService.Validate(slip);
        var payment = new TransferPayment { StockTransferId = id, RequestKey = key, Amount = amount, PaidAt = paidAt, SubmittedAt = clock.GetUtcNow().UtcDateTime, Reference = reference.Trim(), Note = note, SubmittedBy = uid, Status = TransferPaymentStatus.Submitted, Slip = slip, SlipType = type };
        db.TransferPayments.Add(payment); t.FinancialStatus = TransferFinancialStatus.Submitted; audit.Add("Transfer.PaymentSubmitted", id, $"{amount:N2}; {reference}");
        await notifications.AddAsync(null, $"transfer:{id}:payment:{key}", $"{t.PoNumber} แจ้งชำระ {amount:N2} บาท รอฝ่ายการเงินตรวจ", "/Transfers/Detail/" + id);
        await db.SaveChangesAsync(); await tx.CommitAsync(); return payment.Id;
    }
    public async Task ReviewPaymentAsync(ClaimsPrincipal user, long paymentId, bool approve, string reason)
    {
        if (!user.IsInRole("Finance") && !user.IsInRole("Admin") && !user.IsInRole("SuperAdmin")) throw new BusinessException("เฉพาะฝ่ายการเงินตรวจยอดได้"); Note(reason); var uid = UserId(user);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var p = await db.TransferPayments.Include(x => x.StockTransfer).ThenInclude(x => x.DestinationWarehouse).SingleOrDefaultAsync(x => x.Id == paymentId) ?? throw new BusinessException("ไม่พบรายการชำระ");
        if (p.Status != TransferPaymentStatus.Submitted) return; p.Status = approve ? TransferPaymentStatus.Confirmed : TransferPaymentStatus.Rejected; p.ReviewedAt = clock.GetUtcNow().UtcDateTime; p.ReviewedBy = uid; p.ReviewNote = reason;
        var t = p.StockTransfer; if (approve) { t.PaidAmount += p.Amount; t.FinancialStatus = t.PaidAmount == t.ChargeAmount ? TransferFinancialStatus.Paid : TransferFinancialStatus.AwaitingPayment; t.FinancialApprovedAt = p.ReviewedAt; t.FinancialApprovedBy = uid; } else t.FinancialStatus = TransferFinancialStatus.Rejected;
        audit.Add("Transfer.PaymentReviewed", p.Id, $"approve={approve}; {reason}"); await notifications.AddAsync(t.DestinationWarehouse.OwnerMemberId, $"transfer:{t.Id}:payment-reviewed:{p.Id}", approve ? $"ยืนยันรับเงิน {t.PoNumber} แล้ว {p.Amount:N2} บาท" : $"หลักฐานชำระ {t.PoNumber} ไม่ผ่าน: {reason}", "/Transfers/Detail/" + t.Id);
        await db.SaveChangesAsync(); await tx.CommitAsync();
    }
    public async Task ApproveFinancialAsync(ClaimsPrincipal user, long id, string reference, DateTime? dueAt, string reason)
    {
        if (!user.IsInRole("Finance") && !user.IsInRole("Admin") && !user.IsInRole("SuperAdmin")) throw new BusinessException("เฉพาะฝ่ายการเงินอนุมัติได้"); Note(reason); var uid = UserId(user);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable); var t = await Load(id);
        if (t.PaymentMethod == TransferPaymentMethod.BankTransfer || t.PaymentMethod == TransferPaymentMethod.NoCharge) throw new BusinessException("วิธีชำระนี้ต้องใช้ขั้นตอนอื่น");
        if (t.PaymentMethod == TransferPaymentMethod.MemberCredit)
        {
            var memberId = t.DestinationWarehouse.OwnerMemberId ?? throw new BusinessException("ไม่พบสมาชิกปลายทาง");
            var account = await db.CreditAccounts.SingleOrDefaultAsync(x => x.MemberId == memberId && x.Enabled) ?? throw new BusinessException("สมาชิกไม่มีวงเงินเครดิตที่เปิดใช้งาน");
            var orderDebt = await db.CreditInvoices.Where(x => x.CreditAccountId == account.Id).SumAsync(x => (decimal?)(x.Amount - x.Paid - x.Adjusted)) ?? 0;
            var transferDebt = await db.StockTransfers.Where(x => x.Id != t.Id && x.DestinationWarehouse.OwnerMemberId == memberId && x.PaymentMethod == TransferPaymentMethod.MemberCredit && x.FinancialStatus == TransferFinancialStatus.Approved).SumAsync(x => (decimal?)(x.ChargeAmount - x.PaidAmount)) ?? 0;
            if (orderDebt + transferDebt + t.ChargeAmount > account.Limit) throw new BusinessException("วงเงินเครดิตคงเหลือไม่เพียงพอ");
            t.DueAt = dueAt ?? clock.GetUtcNow().UtcDateTime.AddDays(account.TermDays);
        }
        else { if (string.IsNullOrWhiteSpace(reference)) throw new BusinessException("ระบุเลขอ้างอิงรับเงินสด"); t.PaidAmount = t.ChargeAmount; t.FinancialStatus = TransferFinancialStatus.Paid; }
        if (t.PaymentMethod == TransferPaymentMethod.MemberCredit) t.FinancialStatus = TransferFinancialStatus.Approved;
        t.FinancialApprovedAt = clock.GetUtcNow().UtcDateTime; t.FinancialApprovedBy = uid; t.FinancialNote = $"{reference}; {reason}"; audit.Add("Transfer.FinancialApproved", id, t.FinancialNote);
        await notifications.AddAsync(t.DestinationWarehouse.OwnerMemberId, $"transfer:{id}:finance", $"ฝ่ายการเงินอนุมัติ {t.PoNumber} แล้ว", "/Transfers/Detail/" + id); await db.SaveChangesAsync(); await tx.CommitAsync();
    }
}
