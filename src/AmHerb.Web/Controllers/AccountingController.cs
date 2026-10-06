using AmHerb.Web.Data;
using AmHerb.Web.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace AmHerb.Web.Controllers;
[Authorize(Policy="Payments")]
public class AccountingController(AmHerbDbContext db):Controller
{
    public async Task<IActionResult> Index(DateTime? from,DateTime? to)
    {
        var start=(from??DateTime.UtcNow.AddHours(7).Date.AddDays(-30)).Date;var end=(to??DateTime.UtcNow.AddHours(7).Date).Date;
        if(end<start||end-start>TimeSpan.FromDays(366))throw new AmHerb.Web.Services.BusinessException("เลือกช่วงวันที่ไม่เกิน 366 วัน");
        ViewBag.From=start;ViewBag.To=end;var utcStart=start.AddHours(-7);var utcEnd=end.AddDays(1).AddHours(-7);
        return View(await db.JournalEntries.AsNoTracking().Include(x=>x.Lines).Include(x=>x.Order).Where(x=>x.PostedAt>=utcStart&&x.PostedAt<utcEnd).OrderBy(x=>x.PostedAt).ThenBy(x=>x.Id).ToListAsync());
    }
}
