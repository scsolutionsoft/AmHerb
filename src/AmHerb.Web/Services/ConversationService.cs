using System.Security.Claims;
using AmHerb.Web.Data;
using AmHerb.Web.Domain;
using Microsoft.EntityFrameworkCore;
namespace AmHerb.Web.Services;
public class ConversationService(AmHerbDbContext db,TimeProvider clock,INotificationService notifications,AuditService audit)
{
    public static bool Staff(ClaimsPrincipal user)=>new[]{"SuperAdmin","Admin","CustomerService"}.Any(user.IsInRole);
    public IQueryable<Conversation> Visible(ClaimsPrincipal user){var uid=user.FindFirstValue(ClaimTypes.NameIdentifier);return Staff(user)?db.Conversations:db.Conversations.Where(x=>x.Member.UserId==uid&&x.Member.Status==MemberStatus.Active);}
    public async Task<long> Send(ClaimsPrincipal user,long? id,long? memberId,string? subject,string body,Guid key)
    {
        var uid=user.FindFirstValue(ClaimTypes.NameIdentifier)??throw new BusinessException("กรุณาเข้าสู่ระบบ");var staff=Staff(user);
        if(string.IsNullOrWhiteSpace(body)||body.Length>4000||key==Guid.Empty)throw new BusinessException("ข้อความต้องมี 1–4,000 ตัวอักษร");
        await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var previous=await db.ConversationMessages.SingleOrDefaultAsync(x=>x.RequestKey==key);
        if(previous!=null){if(previous.SenderUserId!=uid||previous.Body!=body||(id!=null&&previous.ConversationId!=id))throw new BusinessException("เลขอ้างอิงข้อความซ้ำ");return previous.ConversationId;}
        Conversation thread;
        if(id==null){
            if(string.IsNullOrWhiteSpace(subject)||subject.Length>180)throw new BusinessException("กรุณาระบุหัวข้อไม่เกิน 180 ตัวอักษร");
            var member=await db.Members.SingleOrDefaultAsync(x=>x.Status==MemberStatus.Active&&(staff?x.Id==memberId:x.UserId==uid))??throw new BusinessException("ไม่พบสมาชิกที่ติดต่อได้");
            thread=new Conversation{MemberId=member.Id,Subject=subject};db.Conversations.Add(thread);
        } else thread=await Visible(user).SingleOrDefaultAsync(x=>x.Id==id)??throw new BusinessException("ไม่พบการสนทนาหรือไม่มีสิทธิ์");
        if(thread.Status==ConversationStatus.Closed)throw new BusinessException("เรื่องนี้ปิดแล้ว กรุณาเปิดเรื่องใหม่");
        var now=clock.GetUtcNow().UtcDateTime;thread.Status=staff?ConversationStatus.WaitingForMember:ConversationStatus.WaitingForStaff;thread.UpdatedAt=now;
        thread.Messages.Add(new ConversationMessage{RequestKey=key,SenderUserId=uid,FromStaff=staff,Body=body,SentAt=now});await db.SaveChangesAsync();
        await notifications.AddAsync(staff?thread.MemberId:null,"message:"+key,$"ข้อความใหม่ในเรื่อง #{thread.Id}: {thread.Subject}");
        audit.Add("Conversation.Send",thread.Id,staff?"Staff reply":"Member message");await db.SaveChangesAsync();await tx.CommitAsync();return thread.Id;
    }
    public async Task Read(ClaimsPrincipal user,long id)
    {
        if(!await Visible(user).AnyAsync(x=>x.Id==id))throw new BusinessException("ไม่มีสิทธิ์อ่านข้อความ");var staff=Staff(user);var now=clock.GetUtcNow().UtcDateTime;
        await db.ConversationMessages.Where(x=>x.ConversationId==id&&x.FromStaff!=staff&&x.ReadAt==null).ExecuteUpdateAsync(x=>x.SetProperty(m=>m.ReadAt,now));
    }
    public async Task Close(ClaimsPrincipal user,long id,string reason)
    { if(string.IsNullOrWhiteSpace(reason)||reason.Length>500)throw new BusinessException("กรุณาระบุเหตุผลปิดเรื่องไม่เกิน 500 ตัวอักษร");var thread=await Visible(user).SingleOrDefaultAsync(x=>x.Id==id)??throw new BusinessException("ไม่พบการสนทนา");if(thread.Status==ConversationStatus.Closed)return;thread.Status=ConversationStatus.Closed;thread.UpdatedAt=clock.GetUtcNow().UtcDateTime;audit.Add("Conversation.Close",id,reason);await notifications.AddAsync(Staff(user)?thread.MemberId:null,"conversation:closed:"+id,$"ปิดการติดต่อ #{id}: {thread.Subject}");await db.SaveChangesAsync(); }
}
