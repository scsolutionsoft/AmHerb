using AmHerb.Web.Data;
using AmHerb.Web.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace AmHerb.Web.ViewComponents;
public record ProductImageEditorModel(long ProductId, string Name, List<MediaAsset> Images);
public class ProductImageEditorViewComponent(AmHerbDbContext db):ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(long productId)
    {
        var name=await db.Products.Where(x=>x.Id==productId).Select(x=>x.Name).SingleAsync();
        var images=await db.MediaAssets.Where(x=>x.ProductId==productId).OrderBy(x=>x.Slot)
            .Select(x=>new MediaAsset{Id=x.Id,Slot=x.Slot,Alt=x.Alt}).ToListAsync();
        return View(new ProductImageEditorModel(productId,name,images));
    }
}
