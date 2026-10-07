using System.Security.Claims;
using AmHerb.Web.Data;
using AmHerb.Web.Domain;
using AmHerb.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AmHerb.Web.Controllers;
[Authorize]
public class MessagesController(AmHerbDbContext db, ConversationService conversations) : Controller
{
    public static string Label(ConversationStatus s) => s switch { ConversationStatus.WaitingForStaff => "รอผู้รับตอบ", ConversationStatus.WaitingForMember => "รอสมาชิกตอบ", _ => "ปิดเรื่องแล้ว" };
    public async Task<IActionResult> Index()
    {
        var staff = ConversationService.Staff(User); var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
        ViewBag.Staff = staff;
        ViewBag.Members = await db.Members.AsNoTracking().Where(x => x.Status == MemberStatus.Active && (staff || x.UserId != uid)).OrderBy(x => x.Name).ToListAsync();
        return View(await conversations.Visible(User).AsNoTracking().Include(x => x.Member).Include(x => x.OtherMember).OrderByDescending(x => x.UpdatedAt).Take(100).ToListAsync());
    }
    public async Task<IActionResult> Detail(long id)
    {
        var thread = await conversations.Visible(User).AsNoTracking().Include(x => x.Member).Include(x => x.OtherMember).Include(x => x.Messages).SingleOrDefaultAsync(x => x.Id == id);
        if (thread == null) return NotFound(); return View(thread);
    }
    [HttpPost, RequestSizeLimit(9 * 1024 * 1024)]
    public async Task<IActionResult> Send(long? id, long? memberId, long? recipientMemberId, string? subject, string body, Guid key, IFormFile? attachment)
    {
        byte[]? data = null;
        if (attachment != null) { if (attachment.Length > 8 * 1024 * 1024) throw new BusinessException("ไฟล์แนบต้องมีขนาดไม่เกิน 8 MB"); using var stream = new MemoryStream(); await attachment.CopyToAsync(stream); data = stream.ToArray(); }
        var thread = await conversations.Send(User, id, memberId, recipientMemberId, subject, body, key, attachment?.FileName, data); return RedirectToAction(nameof(Detail), new { id = thread });
    }
    [HttpPost] public async Task<IActionResult> Read(long id) { await conversations.Read(User, id); return Ok(); }
    [HttpPost] public async Task<IActionResult> Close(long id, string reason) { await conversations.Close(User, id, reason); return RedirectToAction(nameof(Detail), new { id }); }
    public async Task<IActionResult> Unread()
    {
        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier); var ids = conversations.Visible(User).Select(x => x.Id);
        return Json(new { count = await db.ConversationMessages.CountAsync(x => ids.Contains(x.ConversationId) && x.SenderUserId != uid && x.ReadAt == null) });
    }
    public async Task<IActionResult> Attachment(long id, bool download = false)
    {
        var item = await db.ConversationMessages.AsNoTracking().Where(x => x.Id == id && x.AttachmentData != null).Select(x => new { x.ConversationId, x.AttachmentData, x.AttachmentContentType, x.AttachmentName }).SingleOrDefaultAsync();
        if (item == null || !await conversations.Visible(User).AnyAsync(x => x.Id == item.ConversationId)) return NotFound();
        Response.Headers.CacheControl = "private, no-store";
        return download ? File(item.AttachmentData!, item.AttachmentContentType, item.AttachmentName) : File(item.AttachmentData!, item.AttachmentContentType);
    }
}
