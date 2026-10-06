using AmHerb.Web.Data;
using AmHerb.Web.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace AmHerb.Web.ViewComponents;
public class ProductGalleryViewComponent(AmHerbDbContext db):ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(long productId)=>View(await db.MediaAssets.Where(x=>x.ProductId==productId).OrderBy(x=>x.Slot).Select(x=>new MediaAsset{Id=x.Id,Slot=x.Slot,Alt=x.Alt}).ToListAsync());
}
