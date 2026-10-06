using System.ComponentModel.DataAnnotations;
using AmHerb.Web.Domain;
namespace AmHerb.Web.Models;
public class ProductInput
{
    public long? SkuId { get; set; }
    public long? ProductId { get; set; }
    [Required, MaxLength(160)] public string Name { get; set; } = "";
    [Required, MaxLength(80)] public string Code { get; set; } = "";
    [MaxLength(80)] public string Barcode { get; set; } = "";
    [Required, MaxLength(160)] public string Variant { get; set; } = "มาตรฐาน";
    [MaxLength(100)] public string Category { get; set; } = "";
    [MaxLength(4000)] public string Description { get; set; } = "";
    [MaxLength(500)] public string ImageUrl { get; set; } = "";
    [MaxLength(100)] public string RegistrationNumber { get; set; } = "";
    [MaxLength(200)] public string Manufacturer { get; set; } = "";
    [MaxLength(200)] public string Distributor { get; set; } = "AM HERB";
    [MaxLength(2000)] public string Warnings { get; set; } = "";
    [MaxLength(500)] public string LabelDocumentUrl { get; set; } = "";
    [Range(0, 10000)] public int? CapsuleCount { get; set; }
    [Range(0, 100000)] public decimal? WeightGrams { get; set; }
    public bool Active { get; set; } = true;
    public bool TokenEligible { get; set; } = true;
    public bool LotRequired { get; set; } = true;
    [Range(0, 100000)] public int LowStockThreshold { get; set; } = 10;
    [Range(0.01, 1000000)] public decimal InitialPrice { get; set; }
    [Range(0, 1000000)] public decimal InitialToken { get; set; }
    [Required, MaxLength(1000)] public string Reason { get; set; } = "";
    public string? RowVersion { get; set; }
}
public class PromotionInput
{
    [Required, MaxLength(60)] public string Code { get; set; } = "";
    public PromotionKind Kind { get; set; }
    [Range(0, 1000000)] public decimal Value { get; set; }
    [Range(0, 1000000)] public decimal MinimumSpend { get; set; }
    [Range(1, 10000)] public int RequiredQuantity { get; set; } = 1;
    public long? EligibleSkuId { get; set; }
    public long? GiftSkuId { get; set; }
    public SalesChannel? Channel { get; set; }
    public MemberType? MemberType { get; set; }
    [Range(1, 1000000)] public int UsageLimit { get; set; } = 100;
    public DateTime EffectiveFrom { get; set; }
    public DateTime EffectiveTo { get; set; }
    [Range(0, 10)] public decimal TokenMultiplier { get; set; } = 1;
    [Required, MaxLength(1000)] public string Reason { get; set; } = "";
}
