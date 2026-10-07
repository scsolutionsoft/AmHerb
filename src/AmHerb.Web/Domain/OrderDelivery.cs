using System.ComponentModel.DataAnnotations;
namespace AmHerb.Web.Domain;

public class ShippingProvider : Entity
{
    [MaxLength(120)] public string Name { get; set; } = "";
    [MaxLength(120)] public string NormalizedName { get; set; } = "";
}
public class StoreShippingOption : Entity
{
    public long StoreId { get; set; }
    public MemberStore Store { get; set; } = null!;
    public long ShippingProviderId { get; set; }
    public ShippingProvider ShippingProvider { get; set; } = null!;
    public bool Enabled { get; set; }
}
public class PaymentSlip : Entity
{
    public long OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public byte[] Data { get; set; } = [];
    [MaxLength(40)] public string ContentType { get; set; } = "";
    [MaxLength(64)] public string Hash { get; set; } = "";
    [MaxLength(200)] public string Reference { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}
