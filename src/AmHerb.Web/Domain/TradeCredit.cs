using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace AmHerb.Web.Domain;
public class CreditAccount : VersionedEntity
{
    public long MemberId { get; set; }
    public Member Member { get; set; } = null!;
    public decimal Limit { get; set; }
    public int TermDays { get; set; } = 30;
    public bool Enabled { get; set; }
    public bool MasterDealer { get; set; }
    public DateTime UpdatedAt { get; set; }
}
public class CreditInvoice : VersionedEntity
{
    public long CreditAccountId { get; set; }
    public CreditAccount CreditAccount { get; set; } = null!;
    public long OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public decimal Amount { get; set; }
    public decimal Paid { get; set; }
    public decimal Adjusted { get; set; }
    [NotMapped] public decimal Balance => Amount - Paid - Adjusted;
    public DateTime IssuedAt { get; set; }
    public DateTime DueAt { get; set; }
}
public enum CreditReceiptStatus { Pending, Confirmed, Rejected }
public class CreditReceipt : VersionedEntity
{
    public long CreditAccountId { get; set; }
    public CreditAccount CreditAccount { get; set; } = null!;
    public Guid RequestKey { get; set; }
    public decimal Amount { get; set; }
    public DateTime TransferredAt { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    [MaxLength(200)] public string Reference { get; set; } = "";
    [MaxLength(200)] public string? ConfirmedReference { get; set; }
    [MaxLength(1000)] public string Note { get; set; } = "";
    [MaxLength(1000)] public string ReviewNote { get; set; } = "";
    [MaxLength(450)] public string SubmittedBy { get; set; } = "";
    [MaxLength(450)] public string ReviewedBy { get; set; } = "";
    public CreditReceiptStatus Status { get; set; }
    public byte[]? Slip { get; set; }
    [MaxLength(40)] public string SlipType { get; set; } = "";
}
public class CreditAllocation : Entity
{
    public long CreditReceiptId { get; set; }
    public CreditReceipt CreditReceipt { get; set; } = null!;
    public long CreditInvoiceId { get; set; }
    public CreditInvoice CreditInvoice { get; set; } = null!;
    public decimal Amount { get; set; }
}
