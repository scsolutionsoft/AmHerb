using AmHerb.Web.Data;
using AmHerb.Web.Domain;
using AmHerb.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace AmHerb.Web.Controllers;
public class MediaController(AmHerbDbContext db, MediaService media, TimeProvider clock, AuditService audit) : Controller
{
    private IActionResult ReturnToEditor(long productId, string? returnUrl) => Url.IsLocalUrl(returnUrl)
        ? LocalRedirect(returnUrl!) : RedirectToAction(nameof(Product), new { id = productId });
    [HttpPost, Authorize(Policy="Prices")] public async Task<IActionResult> Caption(long id, string? alt, string? returnUrl)
    {
        var product = await db.MediaAssets.Where(x=>x.Id==id&&x.ProductId!=null).Select(x=>x.ProductId).SingleOrDefaultAsync();
        if(product==null)return NotFound();
        await media.EditCaption(id,alt??""); TempData["Success"]="บันทึกคำอธิบายภาพแล้ว";
        return ReturnToEditor(product.Value,returnUrl);
    }
    [HttpPost, Authorize(Policy="Prices")] public async Task<IActionResult> Move(long id, int slot, string? returnUrl)
    {
        var product = await db.MediaAssets.Where(x=>x.Id==id&&x.ProductId!=null).Select(x=>x.ProductId).SingleOrDefaultAsync();
        if(product==null)return NotFound();
        await media.Move(id,slot); TempData["Success"]="จัดลำดับภาพแล้ว ภาพตำแหน่งแรกใช้แสดงในรายการสินค้า";
        return ReturnToEditor(product.Value,returnUrl);
    }
    public async Task<IActionResult> Image(long id)
    {
        var now=clock.GetUtcNow().UtcDateTime; var member=User.Identity?.IsAuthenticated==true;
        var manager=new[]{"SuperAdmin","Admin","Marketing"}.Any(User.IsInRole);
        var allowed=db.MediaAssets.Where(x=>x.Id==id && (manager || x.Purpose=="brand" || (x.ProductId!=null&&x.Product!.Active) || db.ContentPosts.Any(p=>p.MediaAssetId==x.Id && p.Published&&p.StartsAt<=now&&p.EndsAt>now&&(p.Audience==ContentAudience.Everyone || (member ? p.Audience==ContentAudience.Members : p.Audience==ContentAudience.Public)))));
        var asset=await allowed.AsNoTracking().SingleOrDefaultAsync(); if(asset==null)return NotFound();
        Response.Headers.CacheControl="private, no-store"; Response.Headers.XContentTypeOptions="nosniff";
        return File(asset.Data,asset.ContentType);
    }
    public async Task<IActionResult> Thumb(long id)
    {
        var productId=await db.Skus.Where(x=>x.Id==id).Select(x=>(long?)x.ProductId).SingleOrDefaultAsync();
        if(productId==null)return NotFound();
        var imageId=await db.MediaAssets.Where(x=>x.ProductId==productId&&x.Slot<=4).OrderBy(x=>x.Slot).Select(x=>(long?)x.Id).FirstOrDefaultAsync();
        return imageId==null?Redirect("/images/product-placeholder.svg"):await Image(imageId.Value);
    }
    public async Task<IActionResult> Brand(int id=2)
    { var mediaId=await db.MediaAssets.Where(x=>x.Purpose=="brand"&&x.Slot==id).Select(x=>(long?)x.Id).FirstOrDefaultAsync(); return mediaId==null?Redirect("/images/product-placeholder.svg"):await Image(mediaId.Value); }
    [Authorize(Policy="Prices")] public async Task<IActionResult> Product(long id)
    {
        var product=await db.Products.FindAsync(id); if(product==null)return NotFound();
        ViewBag.Images=await db.MediaAssets.Where(x=>x.ProductId==id).Select(x=>new MediaAsset{Id=x.Id,Slot=x.Slot,Alt=x.Alt}).OrderBy(x=>x.Slot).ToListAsync(); return View(product);
    }
    [HttpPost,Authorize(Policy="Prices"),RequestSizeLimit(6*1024*1024)] public async Task<IActionResult> Upload(long productId,int slot,IFormFile file,string? alt,string? returnUrl)
    { if(file==null)throw new BusinessException("กรุณาเลือกภาพ"); await media.SaveProduct(productId,slot,await MediaService.Read(file),alt??""); TempData["Success"]="บันทึกภาพในฐานข้อมูลแล้ว"; return ReturnToEditor(productId,returnUrl); }
    [HttpPost,Authorize(Policy="Prices")] public async Task<IActionResult> Delete(long id,string? returnUrl)
    { var asset=await db.MediaAssets.SingleOrDefaultAsync(x=>x.Id==id&&x.ProductId!=null); if(asset==null)return NotFound(); var product=asset.ProductId!.Value; db.MediaAssets.Remove(asset); audit.Add("Product.ImageDelete",id,"Delete product image"); await db.SaveChangesAsync(); TempData["Success"]="ลบภาพแล้ว"; return ReturnToEditor(product,returnUrl); }
}
