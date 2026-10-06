using System.ComponentModel.DataAnnotations;
namespace AmHerb.Web.Domain;
public enum TransferStatus { Requested, Dispatched, Received, Cancelled }
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
}
public class TransferAllocation : Entity
{
    public long StockTransferId { get; set; }
    public StockTransfer StockTransfer { get; set; } = null!;
    public long SourceBatchId { get; set; }
    public InventoryBatch SourceBatch { get; set; } = null!;
    public int Quantity { get; set; }
}
