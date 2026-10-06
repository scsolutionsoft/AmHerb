using AmHerb.Web.Data;
using AmHerb.Web.Domain;
using AmHerb.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace AmHerb.Web.Controllers;
[Authorize]
public class MessagesController(AmHerbDbContext db,ConversationService conversations):Controller
{
    public static string Label(ConversationStatus s)=>s switch{ConversationStatus.WaitingForStaff=>"รอเจ้าหน้าที่ตอบ",ConversationStatus.WaitingForMember=>"รอสมาชิกตอบ",_=>"ปิดเรื่องแล้ว"};
    public async Task<IActionResult> Index()
    {ViewBag.Staff=ConversationService.Staff(User);ViewBag.Members=ConversationService.Staff(User)?await db.Members.Where(x=>x.Status==MemberStatus.Active).OrderBy(x=>x.Name).ToListAsync():new List<Member>();return View(await conversations.Visible(User).Include(x=>x.Member).OrderByDescending(x=>x.UpdatedAt).Take(100).ToListAsync());}
    public async Task<IActionResult> Detail(long id)
    {var thread=await conversations.Visible(User).Include(x=>x.Member).Include(x=>x.Messages).SingleOrDefaultAsync(x=>x.Id==id);if(thread==null)return NotFound();return View(thread);}
    [HttpPost]public async Task<IActionResult> Send(long? id,long? memberId,string? subject,string body,Guid key){var thread=await conversations.Send(User,id,memberId,subject,body,key);return RedirectToAction(nameof(Detail),new{id=thread});}
    [HttpPost]public async Task<IActionResult> Read(long id){await conversations.Read(User,id);return Ok();}
    [HttpPost]public async Task<IActionResult> Close(long id,string reason){await conversations.Close(User,id,reason);return RedirectToAction(nameof(Detail),new{id});}
    public async Task<IActionResult> Unread(){var staff=ConversationService.Staff(User);var ids=conversations.Visible(User).Select(x=>x.Id);return Json(new{count=await db.ConversationMessages.CountAsync(x=>ids.Contains(x.ConversationId)&&x.FromStaff!=staff&&x.ReadAt==null)});}
}
