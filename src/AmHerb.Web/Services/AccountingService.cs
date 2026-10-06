using AmHerb.Web.Data;
using AmHerb.Web.Domain;
using Microsoft.EntityFrameworkCore;
namespace AmHerb.Web.Services;
// Internal sales subledger; posting runs inside the commerce transaction.
public class AccountingService(AmHerbDbContext db)
{
    public static void Validate(JournalEntry entry)
    {
        if(entry.Lines.Count<2||entry.Lines.Any(x=>x.Debit<0||x.Credit<0||(x.Debit>0)==(x.Credit>0))||entry.Lines.Sum(x=>x.Debit)!=entry.Lines.Sum(x=>x.Credit))throw new BusinessException("รายการบัญชีเดบิตและเครดิตไม่สมดุล");
    }
    private static void Line(JournalEntry entry,string code,string name,decimal debit=0,decimal credit=0)
    {debit=Money.Round(debit);credit=Money.Round(credit);if(debit==0&&credit==0)return;entry.Lines.Add(new JournalLine{Account=code,Name=name,Debit=debit,Credit=credit});}
    public async Task Sale(Order order,Payment payment)
    {
        var key="sale:"+order.Id;if(await db.JournalEntries.AnyAsync(x=>x.EventKey==key)||db.JournalEntries.Local.Any(x=>x.EventKey==key))return;
        var entry=new JournalEntry{EventKey=key,OrderId=order.Id,PostedAt=payment.ConfirmedAt??order.CreatedAt,Description="ขาย "+order.Number};
        Line(entry,payment.Provider=="TradeCredit"?"1100":payment.Provider=="Cash"?"1010":"1020",payment.Provider=="TradeCredit"?"ลูกหนี้การค้า":payment.Provider=="Cash"?"เงินสด":"ธนาคาร / เงินรับผ่านช่องทาง",debit:payment.Amount);
        Line(entry,"4090","ส่วนลดจาก Token",debit:order.TokenValue);
        Line(entry,"4010","รายได้ขายสินค้า",credit:order.Merchandise-order.Discount);
        Line(entry,"4020","รายได้ค่าจัดส่ง",credit:order.Shipping);Line(entry,"2010","ภาษีขายตามคำสั่งซื้อ",credit:order.Tax);
        var cost=await db.StockAllocations.Where(x=>x.OrderItem.OrderId==order.Id).SumAsync(x=>(decimal?)(x.Quantity*x.InventoryBatch.Cost))??0;
        Line(entry,"5010","ต้นทุนสินค้าขาย",debit:cost);Line(entry,"1030","สินค้าคงเหลือ",credit:cost);
        if(entry.Lines.Count==0)return;Validate(entry);db.JournalEntries.Add(entry);
    }
    public async Task Refund(ReturnRequest request,decimal tokenReference,decimal restoredCost,decimal creditReduction=0)
    {
        var key="refund:"+request.Id;if(await db.JournalEntries.AnyAsync(x=>x.EventKey==key))return;
        var item=request.OrderItem;var order=item.Order;var payment=await db.Payments.SingleAsync(x=>x.OrderId==order.Id);
        var tokenValue=Money.Round(request.TokensRestored*tokenReference);
        var tax=Money.Round(item.Tax*item.ReturnedQuantity/item.Quantity)-Money.Round(item.Tax*(item.ReturnedQuantity-request.Quantity)/item.Quantity);
        var shipping=order.Items.All(x=>x.ReturnedQuantity==x.Quantity)?order.Shipping:0;
        var entry=new JournalEntry{EventKey=key,OrderId=order.Id,PostedAt=request.ApprovedAt??DateTime.UtcNow,Description="คืนสินค้า "+order.Number+" / "+request.Id};
        Line(entry,"4010","รายได้ขายสินค้า",debit:request.CashRefund+tokenValue-tax-shipping);
        Line(entry,"4020","รายได้ค่าจัดส่ง",debit:shipping);Line(entry,"2010","ภาษีขายตามคำสั่งซื้อ",debit:tax);
        Line(entry,"1100","ลูกหนี้การค้า",credit:creditReduction);
        Line(entry,payment.Provider=="Cash"?"1010":"1020",payment.Provider=="Cash"?"เงินสด":"ธนาคาร / เงินรับผ่านช่องทาง",credit:request.CashRefund-creditReduction);Line(entry,"4090","ส่วนลดจาก Token",credit:tokenValue);
        Line(entry,"1030","สินค้าคงเหลือ",debit:restoredCost);Line(entry,"5010","ต้นทุนสินค้าขาย",credit:restoredCost);
        if(entry.Lines.Count==0)return;Validate(entry);db.JournalEntries.Add(entry);
    }
}
