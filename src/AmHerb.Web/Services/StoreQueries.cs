using AmHerb.Web.Data;
using AmHerb.Web.Domain;
using AmHerb.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace AmHerb.Web.Services;
public class StoreQueries(AmHerbDbContext db, PricingService pricing, TimeProvider clock)
{
    public async Task<List<CatalogItem>> CatalogAsync(string? search = null, SalesChannel channel = SalesChannel.Store, long? warehouse = null, IReadOnlyCollection<long>? skuIds = null)
    {
        if (channel == SalesChannel.Store && warehouse == null) warehouse = 1;
        var query = db.Skus.Include(x => x.Product).Where(x => x.Active && x.Product.Active);
        if (skuIds != null) query = query.Where(x => skuIds.Contains(x.Id));
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(x => x.Code.Contains(search) || x.Barcode.Contains(search) || x.Product.Name.Contains(search) || x.Product.Category.Contains(search));
        var result = new List<CatalogItem>(); var now = clock.GetUtcNow().UtcDateTime;
        foreach (var sku in await query.OrderBy(x => x.Product.Name).Take(200).ToListAsync())
        {
            var price = await pricing.PriceAsync(sku.Id, channel); var rate = await pricing.RateAsync(sku.Id, channel);
            var stock = await db.InventoryBatches.Where(x => x.SkuId == sku.Id && x.ExpDate > now && (warehouse == null || x.WarehouseId == warehouse)).SumAsync(x => (int?)x.QtyAvailable) ?? 0;
            result.Add(new(sku.Id, sku.Code, sku.Product.Name, sku.Variant, sku.Product.Category, price.Amount, rate.BaseToken, stock, "/Media/Thumb/" + sku.Id, sku.Barcode));
        }
        return result;
    }
}
public class CartService(AmHerbDbContext db, IHttpContextAccessor accessor, PricingService pricing)
{
    public async Task<Cart> GetAsync(long? storeId = null)
    {
        var context = accessor.HttpContext!;
        var cookie = storeId == null ? "am.cart" : "am.cart.store." + storeId;
        if (Guid.TryParse(context.Request.Cookies[cookie], out var key))
        { var found = await db.Carts.Include(x => x.Items).ThenInclude(x => x.Sku).ThenInclude(x => x.Product).SingleOrDefaultAsync(x => x.PublicId == key && x.StoreId == storeId); if (found != null) return found; }
        var cart = new Cart { StoreId = storeId }; db.Carts.Add(cart); await db.SaveChangesAsync();
        context.Response.Cookies.Append(cookie, cart.PublicId.ToString(), new CookieOptions { HttpOnly = true, Secure = context.Request.IsHttps, SameSite = SameSiteMode.Lax, MaxAge = TimeSpan.FromDays(30), IsEssential = true });
        return cart;
    }
    public async Task<List<CartLine>> LinesAsync(Cart cart)
    {
        var list = new List<CartLine>();
        foreach (var item in cart.Items) list.Add(new(item.SkuId, item.Sku.Product.Name, item.Quantity, (await pricing.PriceAsync(item.SkuId, SalesChannel.Store, item.Tier)).Amount, item.Tier));
        return list;
    }
    public async Task SetAsync(long skuId, int quantity, bool add, string tier = "Retail", long? storeId = null)
    {
        if (quantity < 0 || quantity > 10000 || !new[] { "Retail", "Promo", "Wholesale", "Pack6", "Pack12" }.Contains(tier)) throw new BusinessException("จำนวนสินค้าหรือระดับราคาไม่ถูกต้อง");
        if (tier != "Retail" && accessor.HttpContext?.User.Identity?.IsAuthenticated != true) throw new BusinessException("กรุณาเข้าสู่ระบบก่อนใช้ราคาสมาชิก");
        if (storeId != null && (tier != "Retail" || !await db.StoreProducts.AnyAsync(x => x.StoreId == storeId && x.SkuId == skuId && x.Enabled && x.Store.Published && x.Store.Member.Status == MemberStatus.Active && x.Store.Warehouse.Active))) throw new BusinessException("สินค้านี้ไม่เปิดขายในร้าน");
        var cart = await GetAsync(storeId);
        if (!await db.Skus.AnyAsync(x => x.Id == skuId && x.Active && x.Product.Active)) throw new BusinessException("ไม่พบสินค้า");
        var line = cart.Items.SingleOrDefault(x => x.SkuId == skuId);
        if (line == null && quantity > 0) cart.Items.Add(new CartItem { SkuId = skuId, Quantity = quantity, Tier = tier });
        else if (line != null && quantity == 0) db.CartItems.Remove(line);
        else if (line != null) { line.Quantity = add && line.Tier == tier ? Math.Min(10000, line.Quantity + quantity) : quantity; line.Tier = tier; }
        cart.UpdatedAt = DateTime.UtcNow; await db.SaveChangesAsync();
    }
}
