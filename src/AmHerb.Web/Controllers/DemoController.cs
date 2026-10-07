using System.Text.Json;
using AmHerb.Web.Data;
using AmHerb.Web.Models;
using AmHerb.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AmHerb.Web.Controllers;
[Authorize(Roles = "SuperAdmin")]
public class DemoController(AmHerbDbContext db, DemoDataMaintenance maintenance, IWebHostEnvironment environment, Actor actor) : Controller
{
    public async Task<IActionResult> Index()
    {
        if (!environment.IsDevelopment()) return NotFound();
        var state = await db.SystemSettings.AsNoTracking().Where(x => x.Key == "Demo.State").Select(x => x.Value).SingleOrDefaultAsync();
        if (state == null) return NotFound();
        var scenarioValues = await db.SystemSettings.AsNoTracking().Where(x => x.Key.StartsWith("Demo.Scenario.")).OrderBy(x => x.Key).Select(x => x.Value).ToListAsync();
        var users = await db.Users.AsNoTracking().OrderBy(x => x.UserName).Select(x => new { x.Id, x.Email }).ToListAsync();
        var members = await db.Members.AsNoTracking().ToDictionaryAsync(x => x.UserId, x => x.Name);
        var roleRows = await db.UserRoles.Join(db.Roles, x => x.RoleId, x => x.Id, (userRole, role) => new { userRole.UserId, role.Name }).ToListAsync();
        return View(new DemoPage {
            State = state, Members = members.Count, Products = await db.Products.CountAsync(), Orders = await db.Orders.CountAsync(), Batches = await db.InventoryBatches.CountAsync(),
            Scenarios = scenarioValues.Select(x => JsonSerializer.Deserialize<DemoScenario>(x)!).ToList(),
            Accounts = users.Select(x => new DemoAccount(x.Email ?? "", members.GetValueOrDefault(x.Id, ""), string.Join(", ", roleRows.Where(r => r.UserId == x.Id).Select(r => r.Name)))).ToList()
        });
    }
    [HttpPost]
    public async Task<IActionResult> Reset(string confirmation)
    {
        if (!environment.IsDevelopment()) return NotFound();
        if (confirmation != "RESET DEMO") { TempData["Error"] = "พิมพ์ RESET DEMO เพื่อยืนยันการล้างชุดข้อมูลทดสอบ"; return RedirectToAction(nameof(Index)); }
        await maintenance.ResetAsync(actor.Id);
        TempData["Success"] = "รีเซ็ตข้อมูลทดสอบแล้ว บัญชีแอดมินเดิม สินค้า รูปภาพ และการตั้งค่าก่อนเริ่มทดสอบยังอยู่";
        return RedirectToAction("Overview", "Dashboard");
    }
}
