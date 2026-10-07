using System.ComponentModel.DataAnnotations;
using AmHerb.Web.Domain;
using AmHerb.Web.Services;

namespace AmHerb.Web.Models;

public record CatalogItem(long Id, string Code, string Name, string Variant, string Category, decimal Price, decimal BaseToken, int Stock, string ImageUrl, string Barcode);
public record CatalogModel(IReadOnlyList<CatalogItem> Items, string? Search);
public record ProductDetail(Product Product, IReadOnlyList<CatalogItem> Skus, IReadOnlyList<ProductPrice> Prices, MemberStore? Store = null);
public record CartLine(long SkuId, string Name, int Quantity, decimal UnitPrice, string Tier = "Retail");
public class CartModel
{
    public List<CarrierChoice> Carriers { get; set; } = [];
    public MemberStore? Store { get; set; }
    public List<CartLine> Lines { get; set; } = [];
    public decimal Total => Lines.Sum(x => x.Quantity * x.UnitPrice);
    public CheckoutInput Checkout { get; set; } = new();
    public WalletBalance? Wallet { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal? CreditAvailable { get; set; }
    public int CreditDays { get; set; }
}
public class PosModel
{
    public PosRegister Register { get; set; } = null!;
    public PosSession? Session { get; set; }
    public IReadOnlyList<CatalogItem> Products { get; set; } = [];
    public IReadOnlyList<Order> Orders { get; set; } = [];
    public IReadOnlyList<PosSession> Sessions { get; set; } = [];
    public decimal Sales { get; set; }
    public string Key { get; set; } = Guid.NewGuid().ToString("N");
}
public class PosSaleInput : CheckoutInput
{
    public IFormFile? Slip { get; set; }
    [MaxLength(200)] public string? SlipReference { get; set; }
    public long SessionId { get; set; }
    public long[] SkuIds { get; set; } = [];
    public int[] Quantities { get; set; } = [];
    [Range(0, 100000000)] public decimal Tendered { get; set; }
    public string PaymentMethod { get; set; } = "Cash";
    public string? BuyerCode { get; set; }
}
public record OrderDetail(Order Order, Payment? Payment, IReadOnlyList<Shipment> Shipments, IReadOnlyList<ReturnRequest> Returns, string PaymentInstructions, bool CanManage);
public class MemberDashboard
{
    public Member Member { get; set; } = null!;
    public WalletBalance Wallet { get; set; } = new(0, 0, 0);
    public int DirectDownlines { get; set; }
    public int ActiveDownlines { get; set; }
    public decimal PersonalSales { get; set; }
    public decimal TeamSales { get; set; }
    public IReadOnlyList<Order> Orders { get; set; } = [];
    public IReadOnlyList<TokenLedger> Ledger { get; set; } = [];
    public IReadOnlyList<TreeNode> Tree { get; set; } = [];
    public IReadOnlyList<TreeNode> Upline { get; set; } = [];
    public IReadOnlyList<Notification> Notifications { get; set; } = [];
    public string ReferralUrl { get; set; } = "";
}
public record TreeNode(long Id, long? ParentId, string Code, string Name, int Depth, MemberStatus Status);
public record DownlineDetail(TreeNode Member, IReadOnlyList<TreeNode> Children);
public record MemberSale(string Number, OrderStatus Status, DateTime CreatedAt, decimal Merchandise);
public class RegisterInput
{
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required, StringLength(160)] public string Name { get; set; } = "";
    [Required, StringLength(10, MinimumLength = 5, ErrorMessage = "รหัสผ่านต้องมี 5–10 ตัวอักษร"), DataType(DataType.Password)] public string Password { get; set; } = "";
    [Compare(nameof(Password)), DataType(DataType.Password)] public string ConfirmPassword { get; set; } = "";
    [StringLength(40)] public string? SponsorCode { get; set; }
}
public record TableRow(string[] Cells, string? Link = null);
public class AdminPage
{
    public string Section { get; set; } = "Dashboard";
    public string Title { get; set; } = "จัดการระบบ";
    public string[] Headers { get; set; } = [];
    public List<TableRow> Rows { get; set; } = [];
    public Dictionary<string, string> Metrics { get; set; } = [];
    public List<Sku> Skus { get; set; } = [];
    public List<Warehouse> Warehouses { get; set; } = [];
    public TokenPolicy? Policy { get; set; }
    public int Page { get; set; } = 1;
    public string? Search { get; set; }
    public IReadOnlyList<TreeNode> Tree { get; set; } = [];
}
