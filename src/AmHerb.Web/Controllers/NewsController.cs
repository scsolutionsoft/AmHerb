using AmHerb.Web.Data;
using AmHerb.Web.Domain;
using AmHerb.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace AmHerb.Web.Controllers;
public class NewsController(AmHerbDbContext db, TimeProvider clock, AuditService audit) : Controller
{
    public static IQueryable<ContentPost> Published(AmHerbDbContext db, bool member, DateTime now) => db.ContentPosts.AsNoTracking().Where(x=>x.Published&&x.StartsAt<=now&&x.EndsAt>now&&(x.Audience==ContentAudience.Everyone || (member?x.Audience==ContentAudience.Members:x.Audience==ContentAudience.Public)));
    public async Task<IActionResult> Index() => View(await Published(db,User.Identity?.IsAuthenticated==true,clock.GetUtcNow().UtcDateTime).OrderByDescending(x=>x.StartsAt).Take(100).ToListAsync());
    [Authorize(Policy="Prices")] public async Task<IActionResult> Manage() => View(await db.ContentPosts.AsNoTracking().OrderByDescending(x=>x.Id).Take(200).ToListAsync());
    [Authorize(Policy="Prices")] public async Task<IActionResult> Edit(long? id)
    { var post=id==null?new ContentPost{StartsAt=clock.GetUtcNow().UtcDateTime,EndsAt=clock.GetUtcNow().UtcDateTime.AddMonths(1)}:await db.ContentPosts.FindAsync(id); return post==null?NotFound():View(post); }
    [HttpPost,Authorize(Policy="Prices"),RequestSizeLimit(6*1024*1024)] public async Task<IActionResult> Save(long? id,string title,string body,string? link,ContentAudience audience,ContentKind kind,DateTime startsAt,DateTime endsAt,bool published,string reason,string? version,IFormFile? image)
    {
        if(string.IsNullOrWhiteSpace(title)||title.Length>180||(body?.Length??0)>6000||string.IsNullOrWhiteSpace(reason)||!Enum.IsDefined(audience)||!Enum.IsDefined(kind))throw new BusinessException("กรุณาตรวจหัวข้อ เนื้อหา และเหตุผล");
        if(!string.IsNullOrEmpty(link)&&(!Url.IsLocalUrl(link)||link.Length>300))throw new BusinessException("ลิงก์ต้องเป็นหน้าภายในเว็บไซต์ เช่น /Catalog");
        // HTML datetime-local is entered in Bangkok time, independently of the server time zone.
        var start=DateTime.SpecifyKind(startsAt.AddHours(-7),DateTimeKind.Utc); var end=DateTime.SpecifyKind(endsAt.AddHours(-7),DateTimeKind.Utc);
        if(end<=start)throw new BusinessException("วันสิ้นสุดต้องอยู่หลังวันเริ่มเผยแพร่");
        byte[]? bytes=image==null?null:await MediaService.Read(image);
        await using var tx=await db.Database.BeginTransactionAsync();
        var post=id==null?new ContentPost():await db.ContentPosts.FindAsync(id)??throw new BusinessException("ไม่พบประกาศ");
        if(id==null)db.ContentPosts.Add(post); else {if(version!=Convert.ToBase64String(post.RowVersion))throw new BusinessException("ข้อมูลถูกแก้ไขแล้ว กรุณาเปิดใหม่");}
        post.Title=title;post.Body=body??"";post.Link=link??"/Catalog";post.Audience=audience;post.Kind=kind;post.StartsAt=start;post.EndsAt=end;post.Published=published;
        if(bytes!=null){var asset=new MediaAsset{Purpose="content",Data=bytes,ContentType=MediaService.Validate(bytes),Alt=title};db.MediaAssets.Add(asset);post.MediaAsset=asset;}
        audit.Add("Content.Save",id??0,reason);await db.SaveChangesAsync();await tx.CommitAsync();return RedirectToAction(nameof(Manage));
    }
}
