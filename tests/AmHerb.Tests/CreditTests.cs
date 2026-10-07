using AmHerb.Web.Domain;
using AmHerb.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
namespace AmHerb.Tests;
[Collection("SQL")]
public class CreditTests(SqlFixture fixture)
{
    [SqlFact] public async Task Concurrent_purchases_cannot_exceed_one_shared_credit_limit()
    {
        await using var h=new Harness(fixture);var buyer=await Setup(h,1500);
        async Task<bool> Purchase(){await using var session=new Harness(fixture);try{await session.Commerce.CheckoutAsync(Input(),[new(h.SkuId,1)],buyer.Id,null);return true;}catch(Exception e)when(e is BusinessException||DatabaseConflict.IsDeadlock(e)){return false;}}
        var results=await Task.WhenAll(Purchase(),Purchase());Assert.Single(results.Where(x=>x));
        await using var db=fixture.Db();var used=await db.CreditInvoices.Where(x=>x.CreditAccount.MemberId==buyer.Id).SumAsync(x=>x.Amount-x.Paid-x.Adjusted);Assert.InRange(used,1,1500);
    }
    [SqlFact] public async Task Concurrent_transfer_reviews_post_one_receipt_once()
    {
        await using var h=new Harness(fixture);var buyer=await Setup(h);await h.Commerce.CheckoutAsync(Input(),[new(h.SkuId,1)],buyer.Id,null);
        var account=await h.Db.CreditAccounts.SingleAsync(x=>x.MemberId==buyer.Id);var id=await Service(h).Submit(account.Id,buyer.UserId,false,Guid.NewGuid(),100,h.Clock.Now,"CONCURRENT-"+Guid.NewGuid(),null,null);
        async Task Review(){await using var session=new Harness(fixture);try{await Service(session).Review(id,true,"finance","verified");}catch(Exception e)when(DatabaseConflict.IsDeadlock(e)){} }
        await Task.WhenAll(Review(),Review());await using var db=fixture.Db();Assert.Equal(100,await db.CreditAllocations.Where(x=>x.CreditReceiptId==id).SumAsync(x=>x.Amount));Assert.Equal(1,await db.JournalEntries.CountAsync(x=>x.EventKey.StartsWith("credit-receipt:"+id+":")));
    }
    private static CreditService Service(Harness h)=>new(h.Db,new AuditService(h.Db,new Actor(new HttpContextAccessor())),h.Clock,new NotificationService(h.Db));
    private static CheckoutInput Input()=>new(){CustomerName="Credit buyer",Phone="0900001111",HouseNumber = "99/1", SubdistrictCode = "100101", PostalCode = "10200", ShippingProviderId = 1, Address="Bangkok",UseCredit=true};
    private static async Task<Member> Setup(Harness h,decimal limit=10000)
    {
        await h.InitializeAsync(price:1000);await h.Inventory.ReceiveAsync(h.SkuId,1,"CREDIT",h.Clock.Now.AddDays(-1),h.Clock.Now.AddYears(1),20,200,"credit test");
        var member=await h.MemberAsync();await Service(h).Configure(member.Id,limit,30,true,true,"approved",null);return member;
    }
    [SqlFact] public async Task Credit_sale_partial_transfer_final_transfer_and_statement_journal_reconcile()
    {
        await using var h=new Harness(fixture);var buyer=await Setup(h);var service=Service(h);
        var order=await h.Commerce.CheckoutAsync(Input(),[new(h.SkuId,2)],buyer.Id,null);
        var invoice=await h.Db.CreditInvoices.SingleAsync(x=>x.OrderId==order.Id);var account=await h.Db.CreditAccounts.SingleAsync(x=>x.MemberId==buyer.Id);
        Assert.Equal(OrderStatus.Processing,order.Status);Assert.Equal(order.CashPayable,invoice.Balance);
        Assert.Equal("CreditApproved",(await h.Db.Payments.SingleAsync(x=>x.OrderId==order.Id)).Status);
        var sale=await h.Db.JournalEntries.Include(x=>x.Lines).SingleAsync(x=>x.OrderId==order.Id);AccountingService.Validate(sale);
        Assert.Equal(order.CashPayable,sale.Lines.Single(x=>x.Account=="1100").Debit);Assert.DoesNotContain(sale.Lines,x=>x.Account=="1020");
        var key=Guid.NewGuid();var receipt=await service.Submit(account.Id,buyer.UserId,false,key,500,h.Clock.Now,"BANK-"+Guid.NewGuid(),"partial",EngagementTests.Png);
        Assert.Equal(order.CashPayable,await service.Outstanding(account.Id));
        await service.Review(receipt,true,"finance","bank verified");await service.Review(receipt,true,"finance","retry");
        Assert.Equal(order.CashPayable-500,await service.Outstanding(account.Id));Assert.Single(await h.Db.CreditAllocations.Where(x=>x.CreditReceiptId==receipt).ToListAsync());
        var second=await service.Submit(account.Id,buyer.UserId,false,Guid.NewGuid(),invoice.Balance,h.Clock.Now,"BANK-"+Guid.NewGuid(),"final",null);await service.Review(second,true,"finance","verified");
        Assert.Equal(0,await service.Outstanding(account.Id));Assert.Equal("Confirmed",(await h.Db.Payments.SingleAsync(x=>x.OrderId==order.Id)).Status);
        var journals=await h.Db.JournalEntries.Include(x=>x.Lines).Where(x=>x.OrderId==order.Id).ToListAsync();foreach(var j in journals)AccountingService.Validate(j);
        Assert.Equal(0,journals.SelectMany(x=>x.Lines).Where(x=>x.Account=="1100").Sum(x=>x.Debit-x.Credit));
        var posted=await h.Db.CreditReceipts.SingleAsync(x=>x.Id==receipt);posted.Amount=1;await Assert.ThrowsAsync<InvalidOperationException>(()=>h.Db.SaveChangesAsync());
    }
    [SqlFact] public async Task Credit_limit_cannot_be_overspent_and_failed_checkout_does_not_post_invoice()
    {
        await using var h=new Harness(fixture);var buyer=await Setup(h,1500);await h.Commerce.CheckoutAsync(Input(),[new(h.SkuId,1)],buyer.Id,null);
        await Assert.ThrowsAsync<BusinessException>(()=>h.Commerce.CheckoutAsync(Input(),[new(h.SkuId,1)],buyer.Id,null));
        await using var verify=fixture.Db();Assert.Single(await verify.CreditInvoices.Where(x=>x.CreditAccount.MemberId==buyer.Id).ToListAsync());
        Assert.Single(await verify.Orders.Where(x=>x.BuyerMemberId==buyer.Id).ToListAsync());
    }
    [SqlFact] public async Task Overdue_or_disabled_credit_is_blocked_and_unpaid_retail_cannot_issue_rewards()
    {
        await using var h=new Harness(fixture);var buyer=await Setup(h);var order=await h.Commerce.CheckoutAsync(Input(),[new(h.SkuId,1)],buyer.Id,null);
        await h.Commerce.FulfillAsync(order.Id,"test","tracking",false,"ship");await h.Commerce.FulfillAsync(order.Id,"test","tracking",true,"deliver");
        await Assert.ThrowsAsync<BusinessException>(()=>h.Commerce.VerifyRetailAsync(order.Id,"unpaid"));
        h.Clock.Now=h.Clock.Now.AddDays(32);
        await Assert.ThrowsAsync<BusinessException>(()=>h.Commerce.CheckoutAsync(Input(),[new(h.SkuId,1)],buyer.Id,null));
    }
    [SqlFact] public async Task Transfer_requires_owner_rejects_overpayment_and_duplicate_bank_reference_and_rejection_does_not_restore_credit()
    {
        await using var h=new Harness(fixture);var buyer=await Setup(h);var other=await h.MemberAsync();var service=Service(h);
        await h.Commerce.CheckoutAsync(Input(),[new(h.SkuId,2)],buyer.Id,null);var account=await h.Db.CreditAccounts.SingleAsync(x=>x.MemberId==buyer.Id);
        await Assert.ThrowsAsync<BusinessException>(()=>service.Submit(account.Id,other.UserId,false,Guid.NewGuid(),1,h.Clock.Now,"NOT-OWNER",null,null));
        await Assert.ThrowsAsync<BusinessException>(()=>service.Submit(account.Id,buyer.UserId,false,Guid.NewGuid(),99999,h.Clock.Now,"TOO-MUCH",null,null));
        var reference="BANK-"+Guid.NewGuid();var id=await service.Submit(account.Id,buyer.UserId,false,Guid.NewGuid(),100,h.Clock.Now,reference,null,null);
        var before=await service.Outstanding(account.Id);await service.Review(id,false,"finance","not received");Assert.Equal(before,await service.Outstanding(account.Id));
        var approved=await service.Submit(account.Id,buyer.UserId,false,Guid.NewGuid(),100,h.Clock.Now,reference,null,null);await service.Review(approved,true,"finance","received");
        var duplicate=await service.Submit(account.Id,buyer.UserId,false,Guid.NewGuid(),100,h.Clock.Now,reference,null,null);await Assert.ThrowsAsync<BusinessException>(()=>service.Review(duplicate,true,"finance","duplicate"));
        Assert.Equal(before-100,await service.Outstanding(account.Id));
    }
    [SqlFact] public async Task Return_offsets_remaining_debt_before_refunding_paid_money()
    {
        await using var h=new Harness(fixture);var buyer=await Setup(h);var service=Service(h);var order=await h.Commerce.CheckoutAsync(Input(),[new(h.SkuId,2)],buyer.Id,null);
        var account=await h.Db.CreditAccounts.SingleAsync(x=>x.MemberId==buyer.Id);
        var receipt=await service.Submit(account.Id,buyer.UserId,false,Guid.NewGuid(),500,h.Clock.Now,"BANK-"+Guid.NewGuid(),null,null);await service.Review(receipt,true,"finance","received");
        await h.Commerce.FulfillAsync(order.Id,"test","tracking",false,"ship");await h.Commerce.FulfillAsync(order.Id,"test","tracking",true,"deliver");
        await h.Commerce.RequestReturnAsync(order.Items.Single().Id,2,"return all");var request=await h.Db.ReturnRequests.SingleAsync(x=>x.OrderItem.OrderId==order.Id);
        await h.Commerce.ApproveReturnAsync(request.Id,true,"REF-"+Guid.NewGuid(),"received");Assert.Equal(0,await service.Outstanding(account.Id));
        var entry=await h.Db.JournalEntries.Include(x=>x.Lines).SingleAsync(x=>x.EventKey=="refund:"+request.Id);AccountingService.Validate(entry);
        Assert.Equal(order.CashPayable-500,entry.Lines.Single(x=>x.Account=="1100").Credit);Assert.Equal(500,entry.Lines.Single(x=>x.Account=="1020").Credit);
    }
    [SqlFact] public async Task Limit_edits_require_current_version_and_suspension_blocks_new_credit()
    {
        await using var h=new Harness(fixture);var buyer=await Setup(h);var service=Service(h);var account=await h.Db.CreditAccounts.SingleAsync(x=>x.MemberId==buyer.Id);
        await Assert.ThrowsAsync<BusinessException>(()=>service.Configure(buyer.Id,5000,30,true,false,"stale","wrong"));
        await service.Configure(buyer.Id,5000,15,false,false,"suspend",Convert.ToBase64String(account.RowVersion));
        await Assert.ThrowsAsync<BusinessException>(()=>h.Commerce.CheckoutAsync(Input(),[new(h.SkuId,1)],buyer.Id,null));
    }
}
