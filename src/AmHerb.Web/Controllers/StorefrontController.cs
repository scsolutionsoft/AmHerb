using AmHerb.Web.Data;
using AmHerb.Web.Domain;
using AmHerb.Web.Models;
using AmHerb.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace AmHerb.Web.Controllers;

[Route("s/{slug}")]
public class StorefrontController(AmHerbDbContext db, StoreQueries queries, CartService carts, CommerceService commerce,
    MemberService members, RewardService rewards, Actor actor, OrderAccess access) : Controller
{
    private Task<MemberStore?> Find(string slug) => db.MemberStores.AsNoTracking().SingleOrDefaultAsync(x => x.Slug == slug && x.Published && x.Member.Status == MemberStatus.Active && x.Warehouse.Active && x.Warehouse.OwnerMemberId == x.MemberId);
    [HttpGet("")]
    public async Task<IActionResult> Index(string slug, string? q)
    {
        var store = await Find(slug); if (store == null) return NotFound();
        var ids = await db.StoreProducts.Where(x => x.StoreId == store.Id && x.Enabled).Select(x => x.SkuId).ToListAsync();
        var products = await queries.CatalogAsync(q, warehouse: store.WarehouseId, skuIds: ids);
        return View(new StorefrontPage(store, products, q));
    }
    [HttpGet("cart")]
    public async Task<IActionResult> Cart(string slug)
    {
        var store = await Find(slug); if (store == null) return NotFound();
        var model = new CartModel { Store = store, Lines = await carts.LinesAsync(await carts.GetAsync(store.Id)) };
        model.Carriers = (await new DeliveryService(db).Options(store.Id)).Where(x => x.Enabled).ToList();
        var shipping = await db.ShippingRules.Where(x => x.Active).OrderBy(x => x.Id).FirstAsync();
        model.ShippingFee = model.Total >= shipping.FreeAbove ? 0 : shipping.Fee;
        if (User.Identity?.IsAuthenticated == true)
        {
            var buyer = await members.RequireAsync(actor.Id); model.Wallet = await rewards.BalanceAsync(buyer.Id);
            model.Checkout.CustomerName = buyer.Name; model.Checkout.Phone = buyer.Phone; model.Checkout.Address = buyer.Address; model.Checkout.Email = User.Identity.Name;
        }
        return View("~/Views/Cart/Index.cshtml", model);
    }
    [HttpGet("products/{id:long}")]
    public async Task<IActionResult> Detail(string slug, long id)
    {
        var store = await Find(slug); if (store == null) return NotFound();
        var sku = await db.StoreProducts.Include(x => x.Sku).ThenInclude(x => x.Product).SingleOrDefaultAsync(x => x.StoreId == store.Id && x.SkuId == id && x.Enabled && x.Sku.Active && x.Sku.Product.Active);
        if (sku == null) return NotFound();
        var enabled = await db.StoreProducts.Where(x => x.StoreId == store.Id && x.Enabled && x.Sku.ProductId == sku.Sku.ProductId).Select(x => x.SkuId).ToListAsync();
        var products = await queries.CatalogAsync(warehouse: store.WarehouseId, skuIds: enabled);
        return View("~/Views/Catalog/Detail.cshtml", new ProductDetail(sku.Sku.Product, products, [], store));
    }
    [HttpPost("add")]
    public async Task<IActionResult> Add(string slug, long skuId, int quantity = 1)
    {
        var store = await Find(slug); if (store == null) return NotFound();
        await carts.SetAsync(skuId, quantity, true, storeId: store.Id); return RedirectToAction(nameof(Cart), new { slug });
    }
    [HttpPost("update")]
    public async Task<IActionResult> Update(string slug, long skuId, int quantity)
    {
        var store = await Find(slug); if (store == null) return NotFound();
        if (quantity == 0)
        {
            var cart = await carts.GetAsync(store.Id); var line = cart.Items.SingleOrDefault(x => x.SkuId == skuId);
            if (line != null) { db.CartItems.Remove(line); await db.SaveChangesAsync(); }
        }
        else await carts.SetAsync(skuId, quantity, false, storeId: store.Id);
        return RedirectToAction(nameof(Cart), new { slug });
    }
    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout(string slug, [Bind(Prefix = "Checkout")] CheckoutInput input)
    {
        var store = await Find(slug); if (store == null) return NotFound();
        if (!ModelState.IsValid) { TempData["Error"] = "กรุณาตรวจสอบข้อมูลผู้รับและที่อยู่จัดส่ง"; return RedirectToAction(nameof(Cart), new { slug }); }
        var cart = await carts.GetAsync(store.Id);
        var buyer = User.Identity?.IsAuthenticated == true ? await members.RequireAsync(actor.Id) : null;
        // Bind replays to this browser cart as well as the store; never accept a different guest's checkout key.
        input.Key = "store:" + cart.PublicId.ToString("N") + ":" + input.Key;
        if (input.Key.Length > 100) throw new BusinessException("รหัสคำสั่งซื้อไม่ถูกต้อง");
        var order = await commerce.CheckoutAsync(input, cart.Items.Select(x => new SaleLine(x.SkuId, x.Quantity, x.Tier)).ToArray(), buyer?.Id, store.MemberId, storeId: store.Id);
        db.CartItems.RemoveRange(cart.Items); await db.SaveChangesAsync(); access.GrantGuest(order);
        return RedirectToAction("Detail", "Orders", new { id = order.PublicId });
    }
}

public record StorefrontPage(MemberStore Store, IReadOnlyList<CatalogItem> Products, string? Query);
