using System.Security.Claims;
using System.Text;
using AmHerb.Web.Data;
using AmHerb.Web.Domain;
using Microsoft.EntityFrameworkCore;

namespace AmHerb.Web.Services;
public class ConversationService(AmHerbDbContext db, TimeProvider clock, INotificationService notifications, AuditService audit)
{
    public static bool Staff(ClaimsPrincipal user) => new[] { "SuperAdmin", "Admin", "CustomerService" }.Any(user.IsInRole);
    private static string UserId(ClaimsPrincipal user) => user.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new BusinessException("กรุณาเข้าสู่ระบบ");
    public IQueryable<Conversation> Visible(ClaimsPrincipal user)
    {
        var uid = UserId(user);
        return Staff(user) ? db.Conversations.Where(x => x.OtherMemberId == null) : db.Conversations.Where(x =>
            (x.Member.UserId == uid && x.Member.Status == MemberStatus.Active) ||
            (x.OtherMember != null && x.OtherMember.UserId == uid && x.OtherMember.Status == MemberStatus.Active));
    }
    public Task<long> Send(ClaimsPrincipal user, long? id, long? memberId, string? subject, string body, Guid key) => Send(user, id, memberId, null, subject, body, key);
    public async Task<long> Send(ClaimsPrincipal user, long? id, long? memberId, long? recipientMemberId, string? subject, string body, Guid key, string? attachmentName = null, byte[]? attachment = null)
    {
        var uid = UserId(user); var staff = Staff(user); body = body?.Trim() ?? "";
        if ((body.Length == 0 && attachment == null) || body.Length > 4000 || key == Guid.Empty) throw new BusinessException("ระบุข้อความหรือไฟล์แนบ และข้อความต้องไม่เกิน 4,000 ตัวอักษร");
        var attachmentType = attachment == null ? "" : ValidateAttachment(attachmentName ?? "", attachment);
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var previous = await db.ConversationMessages.SingleOrDefaultAsync(x => x.RequestKey == key);
        if (previous != null) { if (previous.SenderUserId != uid || previous.Body != body || (id != null && previous.ConversationId != id)) throw new BusinessException("เลขอ้างอิงข้อความซ้ำ"); return previous.ConversationId; }
        Conversation thread;
        if (id == null)
        {
            if (string.IsNullOrWhiteSpace(subject) || subject.Length > 180) throw new BusinessException("กรุณาระบุหัวข้อไม่เกิน 180 ตัวอักษร");
            if (staff)
            {
                var member = await db.Members.SingleOrDefaultAsync(x => x.Status == MemberStatus.Active && x.Id == memberId) ?? throw new BusinessException("ไม่พบสมาชิกที่ติดต่อได้");
                thread = new Conversation { MemberId = member.Id, Subject = subject.Trim() };
            }
            else
            {
                var sender = await db.Members.SingleOrDefaultAsync(x => x.Status == MemberStatus.Active && x.UserId == uid) ?? throw new BusinessException("ไม่พบบัญชีสมาชิกที่ใช้งานได้");
                Member? recipient = null;
                if (recipientMemberId != null) recipient = await db.Members.SingleOrDefaultAsync(x => x.Status == MemberStatus.Active && x.Id == recipientMemberId && x.UserId != uid) ?? throw new BusinessException("ไม่พบสมาชิกผู้รับที่ติดต่อได้");
                thread = new Conversation { MemberId = sender.Id, OtherMemberId = recipient?.Id, Subject = subject.Trim() };
            }
            db.Conversations.Add(thread);
        }
        else thread = await Visible(user).Include(x => x.Member).Include(x => x.OtherMember).SingleOrDefaultAsync(x => x.Id == id) ?? throw new BusinessException("ไม่พบการสนทนาหรือไม่มีสิทธิ์");
        if (thread.Status == ConversationStatus.Closed) throw new BusinessException("เรื่องนี้ปิดแล้ว กรุณาเปิดเรื่องใหม่");
        var now = clock.GetUtcNow().UtcDateTime;
        thread.Status = staff ? ConversationStatus.WaitingForMember : ConversationStatus.WaitingForStaff; thread.UpdatedAt = now;
        thread.Messages.Add(new ConversationMessage { RequestKey = key, SenderUserId = uid, FromStaff = staff, Body = body, SentAt = now, AttachmentName = attachment == null ? "" : SafeName(attachmentName!), AttachmentContentType = attachmentType, AttachmentData = attachment });
        await db.SaveChangesAsync();
        long? notifyMember = null;
        if (staff) notifyMember = thread.MemberId;
        else if (thread.OtherMemberId != null) notifyMember = thread.Member.UserId == uid ? thread.OtherMemberId : thread.MemberId;
        await notifications.AddAsync(notifyMember, "message:" + key, $"ข้อความใหม่ในเรื่อง #{thread.Id}: {thread.Subject}", "/Messages/Detail/" + thread.Id);
        audit.Add("Conversation.Send", thread.Id, attachment == null ? "Message" : $"Message with {attachmentType} attachment");
        await db.SaveChangesAsync(); await tx.CommitAsync(); return thread.Id;
    }
    public async Task Read(ClaimsPrincipal user, long id)
    {
        var uid = UserId(user); if (!await Visible(user).AnyAsync(x => x.Id == id)) throw new BusinessException("ไม่มีสิทธิ์อ่านข้อความ"); var now = clock.GetUtcNow().UtcDateTime;
        await db.ConversationMessages.Where(x => x.ConversationId == id && x.SenderUserId != uid && x.ReadAt == null).ExecuteUpdateAsync(x => x.SetProperty(m => m.ReadAt, now));
    }
    public async Task Close(ClaimsPrincipal user, long id, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Length > 500) throw new BusinessException("กรุณาระบุเหตุผลปิดเรื่องไม่เกิน 500 ตัวอักษร"); var thread = await Visible(user).Include(x => x.Member).Include(x => x.OtherMember).SingleOrDefaultAsync(x => x.Id == id) ?? throw new BusinessException("ไม่พบการสนทนา"); if (thread.Status == ConversationStatus.Closed) return; thread.Status = ConversationStatus.Closed; thread.UpdatedAt = clock.GetUtcNow().UtcDateTime; audit.Add("Conversation.Close", id, reason);
        var uid = UserId(user); var target = thread.OtherMemberId == null ? (Staff(user) ? thread.MemberId : null) : (thread.Member.UserId == uid ? thread.OtherMemberId : thread.MemberId);
        await notifications.AddAsync(target, "conversation:closed:" + id, $"ปิดการติดต่อ #{id}: {thread.Subject}", "/Messages/Detail/" + id); await db.SaveChangesAsync();
    }
    public static string ValidateAttachment(string name, byte[] data)
    {
        if (data.Length == 0 || data.Length > 8 * 1024 * 1024) throw new BusinessException("ไฟล์แนบต้องมีขนาดไม่เกิน 8 MB");
        var ext = Path.GetExtension(name).ToLowerInvariant();
        if (data.Length >= 3 && data[0] == 0xff && data[1] == 0xd8 && data[2] == 0xff && ext is ".jpg" or ".jpeg") return "image/jpeg";
        if (data.Length >= 8 && data.Take(8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }) && ext == ".png") return "image/png";
        if (data.Length >= 12 && Encoding.ASCII.GetString(data, 0, 4) == "RIFF" && Encoding.ASCII.GetString(data, 8, 4) == "WEBP" && ext == ".webp") return "image/webp";
        if (data.Length >= 5 && Encoding.ASCII.GetString(data, 0, 5) == "%PDF-" && ext == ".pdf") return "application/pdf";
        if (data.Length >= 4 && data[0] == 0x50 && data[1] == 0x4b && ext is ".docx" or ".xlsx") return ext == ".docx" ? "application/vnd.openxmlformats-officedocument.wordprocessingml.document" : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        if (ext == ".txt" && data.All(x => x is 9 or 10 or 13 || x >= 32)) return "text/plain";
        throw new BusinessException("รองรับ JPEG, PNG, WebP, PDF, TXT, DOCX และ XLSX เท่านั้น");
    }
    private static string SafeName(string name) { var value = Path.GetFileName(name).Trim(); if (value.Length == 0 || value.Length > 240) throw new BusinessException("ชื่อไฟล์ต้องไม่เกิน 240 ตัวอักษร"); return value; }
}
