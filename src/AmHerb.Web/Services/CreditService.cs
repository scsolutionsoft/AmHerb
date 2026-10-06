using System.Data;
using AmHerb.Web.Data;
using AmHerb.Web.Domain;
using Microsoft.EntityFrameworkCore;
namespace AmHerb.Web.Services;
public class CreditService(AmHerbDbContext db, AuditService audit, TimeProvider clock, INotificationService notifications)
{
    private DateTime Now => clock.GetUtcNow().UtcDateTime;
    private static void Amount(decimal amount) { if(amount<=0||amount>100000000||Money.Round(amount)!=amount)throw new BusinessException("ยอดเงินต้องมากกว่า 0 และมีทศนิยมไม่เกิน 2 ตำแหน่ง"); }
    private async Task<CreditAccount> Lock(long memberId) => await db.CreditAccounts.FromSqlInterpolated($"SELECT * FROM CreditAccounts WITH (UPDLOCK,HOLDLOCK) WHERE MemberId={memberId}").AsNoTracking().SingleOrDefaultAsync() ?? throw new BusinessException("สมาชิกยังไม่ได้รับสิทธิ์เครดิต");
    public async Task LockForPurchase(long memberId) { await Lock(memberId); }
    public Task<decimal> Outstanding(long accountId) => db.CreditInvoices.Where(x=>x.CreditAccountId==accountId).SumAsync(x=>x.Amount-x.Paid-x.Adjusted);
    public async Task Configure(long memberId,decimal limit,int days,bool enabled,bool master,string reason,string? version)
    {
        if(limit<0||limit>100000000||Money.Round(limit)!=limit||days<1||days>365||string.IsNullOrWhiteSpace(reason)||reason.Length>1000)throw new BusinessException("ตรวจสอบวงเงิน วันเครดิต และเหตุผล");
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        if(!await db.Members.AnyAsync(x=>x.Id==memberId&&x.Status==MemberStatus.Active))throw new BusinessException("ไม่พบสมาชิกที่พร้อมใช้งาน");
        var account=await db.CreditAccounts.FromSqlInterpolated($"SELECT * FROM CreditAccounts WITH (UPDLOCK,HOLDLOCK) WHERE MemberId={memberId}").SingleOrDefaultAsync();
        if(account==null){account=new CreditAccount{MemberId=memberId};db.CreditAccounts.Add(account);}
        else {
            if(version!=Convert.ToBase64String(account.RowVersion))throw new BusinessException("ข้อมูลวงเงินเปลี่ยนแล้ว กรุณาโหลดหน้าใหม่");
            if(limit<await Outstanding(account.Id))throw new BusinessException("ลดวงเงินต่ำกว่าหนี้คงค้างไม่ได้ กรุณาปิดสิทธิ์ซื้อเพิ่มแทน");
        }
        var before=$"limit={account.Limit},days={account.TermDays},enabled={account.Enabled},master={account.MasterDealer}";
        account.Limit=limit;account.TermDays=days;account.Enabled=enabled;account.MasterDealer=master;account.UpdatedAt=Now;
        audit.Add("Credit.Configure",memberId,$"{before} -> limit={limit},days={days},enabled={enabled},master={master}; {reason}");
        await notifications.AddAsync(memberId,"credit-setting:"+Guid.NewGuid(),$"ปรับสิทธิ์เครดิต: วงเงิน {limit:N2} บาท / {days} วัน / {(enabled?"เปิด":"ปิด")}สิทธิ์");
        await db.SaveChangesAsync();await tx.CommitAsync();
    }
    // Participates in the checkout transaction; no separate commit.
    public async Task Issue(Order order)
    {
        if(db.Database.CurrentTransaction==null)throw new InvalidOperationException("Credit issue requires the checkout transaction.");
        if(order.BuyerMemberId==null||order.Channel!=SalesChannel.Store)throw new BusinessException("เครดิตบริษัทใช้ได้เมื่อสมาชิกเข้าสู่ระบบและซื้อจากหน้าร้านบริษัท");
        Amount(order.CashPayable);
        var account=await Lock(order.BuyerMemberId.Value);
        if(!account.Enabled||account.Limit<=0)throw new BusinessException("บัญชีเครดิตถูกปิดหรือไม่มีวงเงิน");
        if(await db.CreditInvoices.AnyAsync(x=>x.CreditAccountId==account.Id&&x.DueAt<Now&&x.Amount>x.Paid+x.Adjusted))throw new BusinessException("มีหนี้เกินกำหนด กรุณาชำระก่อนซื้อด้วยเครดิตเพิ่ม");
        if(order.CashPayable>account.Limit-await Outstanding(account.Id))throw new BusinessException("วงเงินเครดิตคงเหลือไม่เพียงพอ");
        db.CreditInvoices.Add(new CreditInvoice{CreditAccountId=account.Id,OrderId=order.Id,Amount=order.CashPayable,IssuedAt=Now,
            DueAt=Now.AddHours(7).Date.AddDays(account.TermDays+1).AddHours(-7).AddTicks(-1)});
        audit.Add("Credit.Purchase",order.Id,$"Credit {order.CashPayable:N2}, {account.TermDays} days");
    }
    public async Task<long> Submit(long accountId,string userId,bool finance,Guid key,decimal amount,DateTime transferred,string reference,string? note,byte[]? slip)
    {
        Amount(amount);reference=reference.Trim().ToUpperInvariant();
        if(key==Guid.Empty||reference.Length==0||reference.Length>200||(note?.Length??0)>1000||transferred>Now.AddMinutes(5)||transferred<Now.AddYears(-1))throw new BusinessException("ตรวจสอบวันโอน เลขอ้างอิง และหมายเหตุ");
        var type=slip==null?"":MediaService.Validate(slip);
        var account=await db.CreditAccounts.Include(x=>x.Member).SingleOrDefaultAsync(x=>x.Id==accountId&&(finance||x.Member.UserId==userId))??throw new BusinessException("ไม่พบบัญชีเครดิตของคุณ");
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var previous=await db.CreditReceipts.SingleOrDefaultAsync(x=>x.RequestKey==key);
        if(previous!=null){if(previous.CreditAccountId!=accountId||previous.SubmittedBy!=userId||previous.Amount!=amount||previous.Reference!=reference)throw new BusinessException("รหัสรายการถูกใช้แล้ว");return previous.Id;}
        await Lock(account.MemberId);
        if(amount>await Outstanding(accountId))throw new BusinessException("ยอดโอนเกินยอดหนี้คงค้าง");
        var receipt=new CreditReceipt{CreditAccountId=accountId,RequestKey=key,Amount=amount,TransferredAt=transferred,SubmittedAt=Now,SubmittedBy=userId,Reference=reference,Note=note??"",Slip=slip,SlipType=type};
        db.CreditReceipts.Add(receipt);audit.Add("Credit.TransferSubmitted",accountId,$"{amount:N2}; {reference}");
        await db.SaveChangesAsync();await notifications.AddAsync(account.MemberId,"credit-receipt:"+receipt.Id,"รับแจ้งโอนชำระเครดิตแล้ว รอฝ่ายการเงินตรวจสอบ");await db.SaveChangesAsync();await tx.CommitAsync();return receipt.Id;
    }
    public async Task Review(long id,bool approve,string userId,string reason)
    {
        if(string.IsNullOrWhiteSpace(reason)||reason.Length>1000)throw new BusinessException("ระบุผลตรวจสอบหรือเหตุผล");
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var info=await db.CreditReceipts.AsNoTracking().Where(x=>x.Id==id).Select(x=>new{x.CreditAccount.MemberId}).SingleOrDefaultAsync()??throw new BusinessException("ไม่พบรายการโอน");
        var account=await Lock(info.MemberId);
        var receipt=await db.CreditReceipts.SingleAsync(x=>x.Id==id);
        if(receipt.Status!=CreditReceiptStatus.Pending){if((approve&&receipt.Status==CreditReceiptStatus.Confirmed)||(!approve&&receipt.Status==CreditReceiptStatus.Rejected))return;throw new BusinessException("รายการนี้ตรวจสอบแล้ว");}
        if(approve)
        {
            if(await db.CreditReceipts.AnyAsync(x=>x.ConfirmedReference==receipt.Reference)||await db.Payments.AnyAsync(x=>x.TransactionRef==receipt.Reference&&x.Status=="Confirmed"))throw new BusinessException("เลขอ้างอิงโอนนี้ถูกใช้ยืนยันแล้ว");
            var invoices=await db.CreditInvoices.Include(x=>x.Order).Where(x=>x.CreditAccountId==account.Id&&x.Amount>x.Paid+x.Adjusted).OrderBy(x=>x.DueAt).ThenBy(x=>x.Id).ToListAsync();
            if(receipt.Amount>invoices.Sum(x=>x.Balance))throw new BusinessException("ยอดโอนเกินหนี้ปัจจุบัน กรุณาปฏิเสธรายการและตรวจสอบยอดใหม่");
            var remaining=receipt.Amount;
            foreach(var invoice in invoices)
            {
                var take=Math.Min(remaining,invoice.Balance);if(take<=0)break;
                invoice.Paid+=take;remaining-=take;
                db.CreditAllocations.Add(new CreditAllocation{CreditReceiptId=receipt.Id,CreditInvoiceId=invoice.Id,Amount=take});
                var journal=new JournalEntry{OrderId=invoice.OrderId,EventKey=$"credit-receipt:{receipt.Id}:{invoice.Id}",PostedAt=Now,Description=$"รับโอนชำระลูกหนี้ {invoice.Order.Number} / RC-{receipt.Id:D8}",Lines=[new JournalLine{Account="1020",Name="ธนาคาร / เงินรับผ่านช่องทาง",Debit=take},new JournalLine{Account="1100",Name="ลูกหนี้การค้า",Credit=take}]};
                AccountingService.Validate(journal);db.JournalEntries.Add(journal);
                if(invoice.Balance==0){var payment=await db.Payments.SingleAsync(x=>x.OrderId==invoice.OrderId);payment.Status="Confirmed";payment.ConfirmedAt=Now;}
            }
            receipt.ConfirmedReference=receipt.Reference;receipt.Status=CreditReceiptStatus.Confirmed;
        }
        else receipt.Status=CreditReceiptStatus.Rejected;
        receipt.ReviewNote=reason;receipt.ReviewedBy=userId;receipt.ReviewedAt=Now;
        audit.Add(approve?"Credit.TransferConfirmed":"Credit.TransferRejected",id,reason);
        await notifications.AddAsync(account.MemberId,"credit-reviewed:"+id,approve?$"ยืนยันชำระเครดิต {receipt.Amount:N2} บาท และคืนวงเงินแล้ว":"แจ้งโอนชำระเครดิตไม่ผ่าน: "+reason);
        await db.SaveChangesAsync();await tx.CommitAsync();
    }
    public async Task<decimal> ApplyReturn(Order order,decimal refund)
    {
        var accountMember=await db.CreditInvoices.Where(x=>x.OrderId==order.Id).Select(x=>(long?)x.CreditAccount.MemberId).SingleOrDefaultAsync();
        if(accountMember==null)return 0;
        await Lock(accountMember.Value);
        var invoice=await db.CreditInvoices.SingleAsync(x=>x.OrderId==order.Id);var reduction=Math.Min(refund,invoice.Balance);invoice.Adjusted+=reduction;
        return reduction;
    }
}
