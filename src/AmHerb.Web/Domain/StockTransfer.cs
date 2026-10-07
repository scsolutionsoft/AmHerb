using System.ComponentModel.DataAnnotations;
namespace AmHerb.Web.Domain;
public enum TransferStatus { Requested, Dispatched, Received, Cancelled }
public enum TransferPaymentMethod { NoCharge, BankTransfer, Cash, MemberCredit }
public enum TransferFinancialStatus { AwaitingPayment, Submitted, CreditReview, Approved, Paid, Rejected, Waived, Cancelled }
public enum TransferPaymentStatus { Submitted, Confirmed, Rejected }
public class StockTransfer : VersionedEntity
{
    public Guid RequestKey { get; set; }
    public long SourceWarehouseId { get; set; }
    public Warehouse SourceWarehouse { get; set; } = null!;
    public long DestinationWarehouseId { get; set; }
    public Warehouse DestinationWarehouse { get; set; } = null!;
    public long SkuId { get; set; }
    public Sku Sku { get; set; } = null!;
    public int Quantity { get; set; }
    [MaxLength(40)] public string PoNumber { get; set; } = "";
    [MaxLength(40)] public string PriceTier { get; set; } = "";
    public decimal UnitPrice { get; set; }
    public decimal ChargeAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public TransferPaymentMethod PaymentMethod { get; set; }
    public TransferFinancialStatus FinancialStatus { get; set; }
    public DateTime? DueAt { get; set; }
    public DateTime? FinancialApprovedAt { get; set; }
    [MaxLength(450)] public string? FinancialApprovedBy { get; set; }
    [MaxLength(1000)] public string FinancialNote { get; set; } = "";
    public TransferStatus Status { get; set; }
    [MaxLength(450)] public string RequestedBy { get; set; } = "";
    [MaxLength(450)] public string? DispatchedBy { get; set; }
    [MaxLength(450)] public string? ReceivedBy { get; set; }
    [MaxLength(1000)] public string Reason { get; set; } = "";
    [MaxLength(200)] public string Tracking { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime? DispatchedAt { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public List<TransferAllocation> Allocations { get; set; } = [];
    public List<TransferPayment> Payments { get; set; } = [];
}
public class TransferAllocation : Entity
{
    public long StockTransferId { get; set; }
    public StockTransfer StockTransfer { get; set; } = null!;
    public long SourceBatchId { get; set; }
    public InventoryBatch SourceBatch { get; set; } = null!;
    public int Quantity { get; set; }
}
public class TransferPayment : VersionedEntity
{
    public long StockTransferId { get; set; }
    public StockTransfer StockTransfer { get; set; } = null!;
    public Guid RequestKey { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaidAt { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    [MaxLength(200)] public string Reference { get; set; } = "";
    [MaxLength(1000)] public string Note { get; set; } = "";
    [MaxLength(1000)] public string ReviewNote { get; set; } = "";
    [MaxLength(450)] public string SubmittedBy { get; set; } = "";
    [MaxLength(450)] public string? ReviewedBy { get; set; }
    public TransferPaymentStatus Status { get; set; }
    public byte[]? Slip { get; set; }
    [MaxLength(40)] public string SlipType { get; set; } = "";
}
