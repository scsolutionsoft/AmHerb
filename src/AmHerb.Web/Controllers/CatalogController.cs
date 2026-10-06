using AmHerb.Web.Data;
using AmHerb.Web.Models;
using AmHerb.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AmHerb.Web.Controllers;
public class CatalogController(AmHerbDbContext db, StoreQueries queries) : Controller
{
    public async Task<IActionResult> Index(string? q) => View(new CatalogModel(await queries.CatalogAsync(q), q));
    public async Task<IActionResult> Detail(long id)
    {
        var sku = await db.Skus.Include(x => x.Product).SingleOrDefaultAsync(x => x.Id == id && x.Active && x.Product.Active);
        if (sku == null) return NotFound();
        var catalog = await queries.CatalogAsync(sku.Product.Name);
        var skuIds = await db.Skus.Where(x => x.ProductId == sku.ProductId).Select(x => x.Id).ToListAsync();
        var now = DateTime.UtcNow;
        var tiers = User.Identity?.IsAuthenticated == true ? await db.ProductPrices.Where(x => skuIds.Contains(x.SkuId) && x.EffectiveFrom <= now && (x.EffectiveTo == null || x.EffectiveTo > now)).OrderBy(x => x.Tier).ToListAsync() : [];
        return View(new ProductDetail(sku.Product, catalog.Where(x => skuIds.Contains(x.Id)).ToArray(), tiers));
    }
}
