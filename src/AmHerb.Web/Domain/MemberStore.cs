using System.ComponentModel.DataAnnotations;
namespace AmHerb.Web.Domain;

public class MemberStore : VersionedEntity
{
    public long MemberId { get; set; }
    public Member Member { get; set; } = null!;
    public long WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;
    [MaxLength(60)] public string Slug { get; set; } = "";
    [MaxLength(160)] public string Name { get; set; } = "";
    [MaxLength(2000)] public string Description { get; set; } = "";
    [MaxLength(40)] public string Phone { get; set; } = "";
    public bool Published { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public class StoreProduct : Entity
{
    public long StoreId { get; set; }
    public MemberStore Store { get; set; } = null!;
    public long SkuId { get; set; }
    public Sku Sku { get; set; } = null!;
    public bool Enabled { get; set; }
}
public class StoreExpense : Entity
{
    public long StoreId { get; set; }
    public MemberStore Store { get; set; } = null!;
    public Guid RequestKey { get; set; }
    public DateTime OccurredAt { get; set; }
    [MaxLength(200)] public string Description { get; set; } = "";
    [MaxLength(200)] public string Reference { get; set; } = "";
    public decimal Amount { get; set; }
    public long? ReversesId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
