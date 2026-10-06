using System.Security.Claims;
using AmHerb.Web.Controllers;
using AmHerb.Web.Domain;
using AmHerb.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace AmHerb.Tests;
[Collection("SQL")]
public class EngagementTests(SqlFixture fixture)
{
    public static byte[] Png=>Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+/lX8AAAAASUVORK5CYII=");
    private static ClaimsPrincipal User(Member m,string role="Member")=>new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier,m.UserId),new Claim(ClaimTypes.Role,role)],"Test"));
    private static AuditService Audit(Harness h,ClaimsPrincipal? user=null)=>new(h.Db,new Actor(new HttpContextAccessor{HttpContext=new DefaultHttpContext{User=user??new ClaimsPrincipal()}}));
    [SqlFact] public async Task Product_media_is_binary_in_SQL_with_four_gallery_slots_and_one_description_slot()
    {
        await using var h=new Harness(fixture);await h.InitializeAsync();var product=(await h.Db.Skus.FindAsync(h.SkuId))!.ProductId;var service=new MediaService(h.Db,Audit(h));
        for(var slot=1;slot<=5;slot++)await service.SaveProduct(product,slot,Png,"ภาพ "+slot);
        await service.SaveProduct(product,1,Png,"แทนที่");Assert.Equal(5,await h.Db.MediaAssets.CountAsync(x=>x.ProductId==product));
        Assert.Equal(Png,(await h.Db.MediaAssets.SingleAsync(x=>x.ProductId==product&&x.Slot==1)).Data);
        await Assert.ThrowsAsync<BusinessException>(()=>service.SaveProduct(product,6,Png,"เกินจำนวน"));
        Assert.Throws<BusinessException>(()=>MediaService.Validate(System.Text.Encoding.UTF8.GetBytes("<svg onload='alert(1)'>invalid image</svg>")));
        Assert.Throws<BusinessException>(()=>MediaService.Validate(new byte[5*1024*1024+1]));
    }
    [SqlFact] public async Task Product_images_can_swap_primary_move_to_empty_slot_and_edit_caption_without_losing_images()
    {
        await using var h=new Harness(fixture);await h.InitializeAsync();var product=(await h.Db.Skus.FindAsync(h.SkuId))!.ProductId;
        var service=new MediaService(h.Db,Audit(h));
        await service.SaveProduct(product,1,Png,"first");await service.SaveProduct(product,2,Png,"second");await service.SaveProduct(product,5,Png,"description");
        var second=await h.Db.MediaAssets.SingleAsync(x=>x.ProductId==product&&x.Slot==2);
        await service.Move(second.Id,1);
        Assert.Equal("second",(await h.Db.MediaAssets.SingleAsync(x=>x.ProductId==product&&x.Slot==1)).Alt);
        Assert.Equal("first",second.Alt);
        await service.Move(second.Id,4);Assert.Equal(4,second.Slot);
        await service.EditCaption(second.Id,"updated");Assert.Equal("updated",second.Alt);Assert.Equal(Png,second.Data);
        Assert.Equal(3,await h.Db.MediaAssets.CountAsync(x=>x.ProductId==product));
        var description=await h.Db.MediaAssets.SingleAsync(x=>x.ProductId==product&&x.Slot==5);
        await Assert.ThrowsAsync<BusinessException>(()=>service.Move(description.Id,1));
        await Assert.ThrowsAsync<BusinessException>(()=>service.Move(second.Id,5));
        await Assert.ThrowsAsync<BusinessException>(()=>service.EditCaption(second.Id,new string('a',201)));
    }
    [SqlFact] public async Task Scheduled_content_and_its_image_are_scoped_by_audience_and_dates()
    {
        await using var h=new Harness(fixture);await h.InitializeAsync();var member=await h.MemberAsync();var now=h.Clock.Now;
        var asset=new MediaAsset{Purpose="content",Data=Png,ContentType="image/png"};h.Db.MediaAssets.Add(asset);
        var post=new ContentPost{Title="Member only",Audience=ContentAudience.Members,Published=true,StartsAt=now,EndsAt=now.AddDays(1),MediaAsset=asset};h.Db.ContentPosts.Add(post);await h.Db.SaveChangesAsync();
        Assert.False(await NewsController.Published(h.Db,false,now).AnyAsync(x=>x.Id==post.Id));Assert.True(await NewsController.Published(h.Db,true,now).AnyAsync(x=>x.Id==post.Id));Assert.False(await NewsController.Published(h.Db,true,now.AddDays(1)).AnyAsync(x=>x.Id==post.Id));
        var audit=Audit(h);var controller=new MediaController(h.Db,new MediaService(h.Db,audit),h.Clock,audit){ControllerContext=new ControllerContext{HttpContext=new DefaultHttpContext()}};
        Assert.IsType<NotFoundResult>(await controller.Image(asset.Id));controller.HttpContext.User=User(member);Assert.IsType<FileContentResult>(await controller.Image(asset.Id));
        h.Clock.Now=now.AddDays(2);Assert.IsType<NotFoundResult>(await controller.Image(asset.Id));
    }
    [SqlFact] public async Task Conversations_enforce_owner_access_read_receipts_status_and_idempotency()
    {
        await using var h=new Harness(fixture);await h.InitializeAsync();var member=await h.MemberAsync();var other=await h.MemberAsync();var user=User(member);var staff=User(other,"CustomerService");
        var service=new ConversationService(h.Db,h.Clock,new NotificationService(h.Db),Audit(h,user));var key=Guid.NewGuid();
        var id=await service.Send(user,null,null,"สอบถามสินค้า","รายละเอียด",key);Assert.Equal(id,await service.Send(user,null,null,"สอบถามสินค้า","รายละเอียด",key));
        Assert.False(await service.Visible(User(other)).AnyAsync(x=>x.Id==id));await Assert.ThrowsAsync<BusinessException>(()=>service.Send(User(other),id,null,null,"แอบตอบ",Guid.NewGuid()));
        await service.Read(staff,id);Assert.NotNull((await h.Db.ConversationMessages.AsNoTracking().SingleAsync(x=>x.RequestKey==key)).ReadAt);
        await service.Send(staff,id,null,null,"เจ้าหน้าที่ตอบแล้ว",Guid.NewGuid());Assert.Equal(ConversationStatus.WaitingForMember,(await h.Db.Conversations.FindAsync(id))!.Status);
        await service.Read(user,id);Assert.Equal(2,await h.Db.ConversationMessages.CountAsync(x=>x.ConversationId==id&&x.ReadAt!=null));
        await service.Close(user,id,"ได้รับคำตอบแล้ว");await Assert.ThrowsAsync<BusinessException>(()=>service.Send(user,id,null,null,"ปิดแล้ว",Guid.NewGuid()));
    }
    [SqlFact] public async Task Sales_journal_balances_and_refunds_reverse_revenue_cash_and_sellable_cost_once()
    {
        await using var h=new Harness(fixture);await h.InitializeAsync();var seller=await h.MemberAsync();var order=await h.SellAsync(seller,2);
        var entry=await h.Db.JournalEntries.Include(x=>x.Lines).SingleAsync(x=>x.EventKey=="sale:"+order.Id);AccountingService.Validate(entry);Assert.Equal(1980,entry.Lines.Single(x=>x.Account=="4010").Credit);
        await new AccountingService(h.Db).Sale(order,await h.Db.Payments.SingleAsync(x=>x.OrderId==order.Id));await h.Db.SaveChangesAsync();Assert.Equal(1,await h.Db.JournalEntries.CountAsync(x=>x.OrderId==order.Id));
        var item=Assert.Single(order.Items);await h.Commerce.RequestReturnAsync(item.Id,2,"คืนครบ");var request=await h.Db.ReturnRequests.SingleAsync(x=>x.OrderItemId==item.Id);
        await h.Commerce.ApproveReturnAsync(request.Id,true,"REFUND","ตรวจรับแล้ว");await h.Commerce.ApproveReturnAsync(request.Id,true,"REFUND","ตรวจรับแล้ว");
        var entries=await h.Db.JournalEntries.Include(x=>x.Lines).Where(x=>x.OrderId==order.Id).ToListAsync();Assert.Equal(2,entries.Count);foreach(var journal in entries)AccountingService.Validate(journal);
        foreach(var account in entries.SelectMany(x=>x.Lines).GroupBy(x=>x.Account))Assert.Equal(account.Sum(x=>x.Debit),account.Sum(x=>x.Credit));
        entry.Description="Changed";await Assert.ThrowsAsync<InvalidOperationException>(()=>h.Db.SaveChangesAsync());
    }
}
