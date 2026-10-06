using AmHerb.Web.Domain;
namespace AmHerb.Web.Models;
public record CreditRow(long Id,long MemberId,string Code,string Name,bool MasterDealer,bool Enabled,decimal Limit,int Days,decimal Used,decimal Overdue,int Pending);
public class CreditListModel
{
    public bool Finance { get; set; }
    public string? Search { get; set; }
    public int Page { get; set; }
    public int Total { get; set; }
    public decimal Used { get; set; }
    public decimal Overdue { get; set; }
    public List<CreditRow> Rows { get; set; }=[];
}
public record ReceiptRow(long Id,decimal Amount,DateTime TransferredAt,string Reference,CreditReceiptStatus Status,string Note,string ReviewNote,bool HasSlip);
public class CreditAccountModel
{
    public CreditAccount Account { get; set; }=null!;
    public bool Finance { get; set; }
    public List<CreditInvoice> Invoices { get; set; }=[];
    public List<ReceiptRow> Receipts { get; set; }=[];
    public decimal Outstanding => Invoices.Sum(x=>x.Balance);
    public DateTime Now { get; set; }
}
public record CreditStatement(CreditAccount Account,DateTime From,DateTime To,decimal Opening,List<JournalEntry> Entries);
