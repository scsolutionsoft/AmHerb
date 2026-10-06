using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace AmHerb.Web.Domain;

public class AppUser : IdentityUser { }
public abstract class Entity { public long Id { get; set; } }
public abstract class VersionedEntity : Entity { [Timestamp] public byte[] RowVersion { get; set; } = []; }
public enum MemberStatus { Pending, Active, Suspended, Closed }
public enum MemberType { Customer, Member, Reseller, Leader }
public enum SalesChannel { Store, POS, Manual, Shopee, TikTokShop, Lazada }
public enum OrderStatus { Draft, PendingPayment, Paid, Processing, Shipped, Delivered, Completed, Cancelled, ReturnRequested, Returned, Refunded }
public enum LedgerKind { SALE_REWARD, UPLINE_L1, UPLINE_L2, UPLINE_L3, LOYALTY, PROMOTION, RELEASE, RESERVATION, RESERVATION_RELEASE, REDEMPTION, RETURN_REVERSAL, EXPIRY, ADMIN_ADJUSTMENT }
public enum TokenStatus { Pending, Available, Reserved, Redeemed, Reversed, Expired }
public enum InventoryKind { Receive, Sale, Reserve, Release, Return, Adjust, Damage, Expire, TransferOut, TransferIn }
public enum PromotionKind { Fixed, Percentage, Bundle, FreeGift }

public class Member : VersionedEntity
{
    [MaxLength(450)] public string UserId { get; set; } = "";
    public AppUser User { get; set; } = null!;
    [MaxLength(40)] public string Code { get; set; } = "";
    [MaxLength(40)] public string ReferralCode { get; set; } = "";
    [MaxLength(160)] public string Name { get; set; } = "";
    [MaxLength(40)] public string Phone { get; set; } = "";
    [MaxLength(1000)] public string Address { get; set; } = "";
    public long? SponsorMemberId { get; set; }
    public Member? Sponsor { get; set; }
    public MemberStatus Status { get; set; } = MemberStatus.Active;
    public MemberType Type { get; set; } = MemberType.Member;
    public bool ReviewRequired { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public class MemberClosure
{
    public long AncestorMemberId { get; set; }
    public Member Ancestor { get; set; } = null!;
    public long DescendantMemberId { get; set; }
    public Member Descendant { get; set; } = null!;
    public int Depth { get; set; }
}
public class Product : VersionedEntity
{
    [Required, MaxLength(160)] public string Name { get; set; } = "";
    [MaxLength(100)] public string Category { get; set; } = "สมุนไพรและผลิตภัณฑ์เสริมอาหาร";
    [MaxLength(4000)] public string Description { get; set; } = "";
    [MaxLength(500)] public string ImageUrl { get; set; } = "";
    [MaxLength(100)] public string RegistrationNumber { get; set; } = "";
    [MaxLength(200)] public string Manufacturer { get; set; } = "";
    [MaxLength(200)] public string Distributor { get; set; } = "AM HERB";
    [MaxLength(2000)] public string Warnings { get; set; } = "อ่านฉลากและคำเตือนก่อนบริโภค";
    [MaxLength(500)] public string LabelDocumentUrl { get; set; } = "";
    public bool Active { get; set; } = true;
    public bool LotRequired { get; set; } = true;
    public List<Sku> Skus { get; set; } = [];
}
public class Sku : VersionedEntity
{
    public long ProductId { get; set; }
    public Product Product { get; set; } = null!;
    [Required, MaxLength(80)] public string Code { get; set; } = "";
    [MaxLength(80)] public string Barcode { get; set; } = "";
    [MaxLength(160)] public string Variant { get; set; } = "มาตรฐาน";
    public int? CapsuleCount { get; set; }
    public decimal? WeightGrams { get; set; }
    public bool Active { get; set; } = true;
    public bool TokenEligible { get; set; } = true;
    public decimal Cost { get; set; }
    public int LowStockThreshold { get; set; } = 10;
}
public class ProductPrice : VersionedEntity
{
    public long SkuId { get; set; }
    public Sku Sku { get; set; } = null!;
    [MaxLength(40)] public string Tier { get; set; } = "Retail";
    public int PackQuantity { get; set; } = 1;
    public SalesChannel? Channel { get; set; }
    public decimal Amount { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}
public class TokenRate : Entity
{
    public long SkuId { get; set; }
    public Sku Sku { get; set; } = null!;
    public SalesChannel? Channel { get; set; }
    public decimal BaseToken { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}
public class TokenPolicy : Entity
{
    public DateTime EffectiveFrom { get; set; }
    public int PendingDays { get; set; } = 14;
    public int ExpiryMonths { get; set; } = 12;
    public decimal SellerPercent { get; set; } = 100;
    public decimal Level1Percent { get; set; } = 20;
    public decimal Level2Percent { get; set; } = 7;
    public decimal Level3Percent { get; set; } = 3;
    public int MaxDepth { get; set; } = 3;
    public decimal RedemptionReferenceThb { get; set; } = 1;
    public decimal MaxRedemptionPercent { get; set; } = 100;
    [MaxLength(1000)] public string RedemptionCategories { get; set; } = "";
    public bool LoyaltyEnabled { get; set; }
    public decimal LoyaltyPercent { get; set; }
    [MaxLength(1000)] public string Reason { get; set; } = "Initial policy";
}
public class ChannelPolicy : VersionedEntity
{
    public SalesChannel Channel { get; set; }
    public bool AllowNetworkReward { get; set; } = true;
    public bool AllowRedemption { get; set; } = true;
}
public enum WarehouseKind { Company, Dealer, Member }
public class Warehouse : Entity
{
    [MaxLength(120)] public string Name { get; set; } = "";
    public bool Active { get; set; } = true;
    public WarehouseKind Kind { get; set; }
    public long? OwnerMemberId { get; set; }
    public Member? OwnerMember { get; set; }
}
public class InventoryBatch : VersionedEntity
{
    public long WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;
    public long SkuId { get; set; }
    public Sku Sku { get; set; } = null!;
    [MaxLength(100)] public string LotNo { get; set; } = "";
    public DateTime MfgDate { get; set; }
    public DateTime ExpDate { get; set; }
    public int QtyReceived { get; set; }
    public int QtyAvailable { get; set; }
    public int QtyReserved { get; set; }
    public decimal Cost { get; set; }
}
public class InventoryTransaction : Entity
{
    public long InventoryBatchId { get; set; }
    public InventoryBatch InventoryBatch { get; set; } = null!;
    public long? OrderId { get; set; }
    public Order? Order { get; set; }
    public InventoryKind Kind { get; set; }
    public int Quantity { get; set; }
    public DateTime PostedAt { get; set; } = DateTime.UtcNow;
    [MaxLength(1000)] public string Reason { get; set; } = "";
}
public class Cart : Entity
{
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public List<CartItem> Items { get; set; } = [];
}
public class CartItem : Entity
{
    public long CartId { get; set; }
    public Cart Cart { get; set; } = null!;
    public long SkuId { get; set; }
    public Sku Sku { get; set; } = null!;
    public int Quantity { get; set; }
    [MaxLength(40)] public string Tier { get; set; } = "Retail";
}
public class Order : VersionedEntity
{
    public Guid PublicId { get; set; } = Guid.NewGuid();
    [MaxLength(60)] public string Number { get; set; } = "";
    [MaxLength(100)] public string IdempotencyKey { get; set; } = "";
    public long? BuyerMemberId { get; set; }
    public Member? Buyer { get; set; }
    public long? SellerMemberId { get; set; }
    public Member? Seller { get; set; }
    [MaxLength(450)] public string? CashierUserId { get; set; }
    public long? PosSessionId { get; set; }
    public PosSession? PosSession { get; set; }
    public SalesChannel Channel { get; set; }
    [MaxLength(100)] public string? ExternalOrderId { get; set; }
    [MaxLength(100)] public string Campaign { get; set; } = "";
    [MaxLength(100)] public string Creator { get; set; } = "";
    public OrderStatus Status { get; set; } = OrderStatus.PendingPayment;
    public bool VerifiedRetailSale { get; set; }
    public bool StockLoading { get; set; }
    public bool RewardsCreated { get; set; }
    [MaxLength(160)] public string CustomerName { get; set; } = "";
    [MaxLength(200)] public string Email { get; set; } = "";
    [MaxLength(40)] public string Phone { get; set; } = "";
    [MaxLength(1000)] public string Address { get; set; } = "";
    public decimal Merchandise { get; set; }
    public decimal Discount { get; set; }
    public decimal Shipping { get; set; }
    public decimal Tax { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal TokenRedemption { get; set; }
    public decimal TokenValue { get; set; }
    public decimal CashPayable { get; set; }
    public decimal ChannelFee { get; set; }
    public decimal AffiliateFee { get; set; }
    public long TokenPolicyVersion { get; set; }
    public long? PromotionId { get; set; }
    public Promotion? Promotion { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public List<OrderItem> Items { get; set; } = [];
}
public class OrderItem : Entity
{
    public long OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public long SkuId { get; set; }
    public Sku Sku { get; set; } = null!;
    [MaxLength(160)] public string Name { get; set; } = "";
    public int Quantity { get; set; }
    public int ReturnedQuantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }
    public decimal TokenValue { get; set; }
    public decimal Tax { get; set; }
    public decimal BaseToken { get; set; }
    public decimal TokenMultiplier { get; set; } = 1;
    public long TokenRateVersion { get; set; }
    public long PriceVersion { get; set; }
    public bool IsGift { get; set; }
    public List<StockAllocation> Allocations { get; set; } = [];
}
public class StockAllocation : Entity
{
    public long OrderItemId { get; set; }
    public OrderItem OrderItem { get; set; } = null!;
    public long InventoryBatchId { get; set; }
    public InventoryBatch InventoryBatch { get; set; } = null!;
    public int Quantity { get; set; }
    public int ReturnedQuantity { get; set; }
}
public class Payment : Entity
{
    public long OrderId { get; set; }
    public Order Order { get; set; } = null!;
    [MaxLength(50)] public string Provider { get; set; } = "ManualBankTransfer";
    [MaxLength(200)] public string? TransactionRef { get; set; }
    [MaxLength(100)] public string WebhookHash { get; set; } = "";
    public decimal Amount { get; set; }
    public decimal Tendered { get; set; }
    public decimal Change { get; set; }
    [MaxLength(30)] public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ConfirmedAt { get; set; }
}
public class TokenDistribution : VersionedEntity
{
    public long MemberId { get; set; }
    public Member Member { get; set; } = null!;
    public long OrderItemId { get; set; }
    public OrderItem OrderItem { get; set; } = null!;
    public long SourceOrderId { get; set; }
    public Order SourceOrder { get; set; } = null!;
    public int Level { get; set; }
    public decimal Amount { get; set; }
    public decimal ReversedAmount { get; set; }
    public decimal ExpiredAmount { get; set; }
    public int QuantityBasis { get; set; }
    public int ReturnedAtIssue { get; set; }
    public TokenStatus Status { get; set; }
    public long RewardPolicyVersion { get; set; }
    public long TokenRateVersion { get; set; }
    public DateTime ReleaseAt { get; set; }
    public DateTime ExpireAt { get; set; }
}
public class TokenLedger : Entity
{
    public long MemberId { get; set; }
    public Member Member { get; set; } = null!;
    public long? SourceOrderId { get; set; }
    public Order? SourceOrder { get; set; }
    public long? OrderItemId { get; set; }
    public OrderItem? OrderItem { get; set; }
    public long? DistributionId { get; set; }
    public TokenDistribution? Distribution { get; set; }
    public LedgerKind Kind { get; set; }
    public TokenStatus Status { get; set; }
    public decimal PendingDelta { get; set; }
    public decimal AvailableDelta { get; set; }
    public decimal ReservedDelta { get; set; }
    public long RewardPolicyVersion { get; set; }
    public long TokenRateVersion { get; set; }
    [MaxLength(160)] public string EventKey { get; set; } = "";
    [MaxLength(1000)] public string Reason { get; set; } = "";
    public DateTime PostedAt { get; set; } = DateTime.UtcNow;
}
// Tracks FIFO consumption of available reward lots for correct expiry after spending.
public class TokenConsumption : Entity
{
    public long DistributionId { get; set; }
    public TokenDistribution Distribution { get; set; } = null!;
    public long OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public decimal Amount { get; set; }
}
public class ReturnRequest : VersionedEntity
{
    public long OrderItemId { get; set; }
    public OrderItem OrderItem { get; set; } = null!;
    public int Quantity { get; set; }
    [MaxLength(1000)] public string Reason { get; set; } = "";
    public bool Sellable { get; set; }
    [MaxLength(30)] public string Status { get; set; } = "Requested";
    public decimal CashRefund { get; set; }
    public decimal TokensRestored { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedAt { get; set; }
    [MaxLength(200)] public string RefundReference { get; set; } = "";
}
public class Shipment : Entity
{
    public long OrderId { get; set; }
    public Order Order { get; set; } = null!;
    [MaxLength(100)] public string Carrier { get; set; } = "";
    [MaxLength(150)] public string TrackingNumber { get; set; } = "";
    [MaxLength(30)] public string Status { get; set; } = "Shipped";
    public DateTime ShippedAt { get; set; } = DateTime.UtcNow;
}
public class ShippingRule : Entity
{
    [MaxLength(100)] public string Name { get; set; } = "จัดส่งมาตรฐาน";
    public decimal Fee { get; set; }
    public decimal FreeAbove { get; set; }
    public bool Active { get; set; } = true;
}
public class Promotion : VersionedEntity
{
    [MaxLength(60)] public string Code { get; set; } = "";
    public PromotionKind Kind { get; set; }
    public decimal Value { get; set; }
    public decimal MinimumSpend { get; set; }
    public int RequiredQuantity { get; set; } = 1;
    public long? EligibleSkuId { get; set; }
    public Sku? EligibleSku { get; set; }
    public long? GiftSkuId { get; set; }
    public Sku? GiftSku { get; set; }
    public SalesChannel? Channel { get; set; }
    public MemberType? MemberType { get; set; }
    public int UsageLimit { get; set; } = 100;
    public int UsedCount { get; set; }
    public bool Active { get; set; } = true;
    public DateTime EffectiveFrom { get; set; }
    public DateTime EffectiveTo { get; set; }
    public decimal TokenMultiplier { get; set; } = 1;
    [MaxLength(1000)] public string Reason { get; set; } = "";
}
public class ReferralVisit : Entity
{
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long MemberId { get; set; }
    public Member Member { get; set; } = null!;
    [MaxLength(100)] public string Source { get; set; } = "";
    [MaxLength(100)] public string Campaign { get; set; } = "";
    [MaxLength(500)] public string LandingPage { get; set; } = "/Catalog";
    public long? ConvertedOrderId { get; set; }
    public Order? ConvertedOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public class Notification : Entity
{
    [MaxLength(450)] public string? UserId { get; set; }
    [MaxLength(160)] public string EventKey { get; set; } = "";
    [MaxLength(1000)] public string Message { get; set; } = "";
    public bool Read { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public class AuditLog : Entity
{
    [MaxLength(450)] public string ActorId { get; set; } = "";
    [MaxLength(120)] public string Action { get; set; } = "";
    [MaxLength(100)] public string Subject { get; set; } = "";
    [MaxLength(2000)] public string Detail { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public class SystemSetting : Entity { [MaxLength(100)] public string Key { get; set; } = ""; [MaxLength(1000)] public string Value { get; set; } = ""; }
public class PosRegister : VersionedEntity
{
    [MaxLength(450)] public string UserId { get; set; } = "";
    public AppUser User { get; set; } = null!;
    public long WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;
    [MaxLength(120)] public string Name { get; set; } = "";
    public bool Enabled { get; set; } = true;
}
public class PosSession : VersionedEntity
{
    public long PosRegisterId { get; set; }
    public PosRegister PosRegister { get; set; } = null!;
    public DateTime OpenedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedAt { get; set; }
    public decimal OpeningCash { get; set; }
    public decimal? CountedCash { get; set; }
    public decimal? ExpectedCash { get; set; }
    public decimal? Difference { get; set; }
    [MaxLength(1000)] public string Note { get; set; } = "";
}
public class ReportSnapshot : Entity
{
    public DateTime Day { get; set; }
    public int Orders { get; set; }
    public decimal Sales { get; set; }
    public decimal TokenLiability { get; set; }
}
public class RedemptionAuthorization : VersionedEntity
{
    public long MemberId { get; set; }
    public Member Member { get; set; } = null!;
    public long PosRegisterId { get; set; }
    public PosRegister PosRegister { get; set; } = null!;
    [MaxLength(64)] public string CodeHash { get; set; } = "";
    public decimal MaximumTokens { get; set; }
    public DateTime ExpiresAt { get; set; }
    public long? UsedOrderId { get; set; }
    public Order? UsedOrder { get; set; }
}
