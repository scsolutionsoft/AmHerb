using AmHerb.Web.Data;
using AmHerb.Web.Domain;
using AmHerb.Web.Models;
using AmHerb.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AmHerb.Web.Controllers;
[Authorize]
public class PosController(AmHerbDbContext db, PosService pos, StoreQueries queries, CommerceService commerce, MemberService members, Actor actor) : Controller
{
    public async Task<IActionResult> Index(string? q)
    {
        var register = await pos.RegisterAsync(actor.Id);
        var session = await db.PosSessions.SingleOrDefaultAsync(x => x.PosRegisterId == register.Id && x.ClosedAt == null);
        return View(new PosModel { Register = register, Session = session, Products = await queries.CatalogAsync(q, SalesChannel.POS, register.WarehouseId),
            Orders = await db.Orders.Where(x => x.CashierUserId == actor.Id).OrderByDescending(x => x.Id).Take(30).ToListAsync(),
            Sessions = await db.PosSessions.Where(x => x.PosRegisterId == register.Id).OrderByDescending(x => x.Id).Take(15).ToListAsync(),
            Sales = session == null ? 0 : await db.Payments.Where(x => x.Order.PosSessionId == session.Id && x.Status == "Confirmed").SumAsync(x => (decimal?)x.Amount) ?? 0 });
    }
    [HttpPost] public async Task<IActionResult> Open(decimal openingCash) { await pos.OpenAsync(actor.Id, openingCash); return RedirectToAction("Index"); }
    [HttpPost] public async Task<IActionResult> Close(long sessionId, decimal countedCash, string note) { await pos.CloseAsync(actor.Id, sessionId, countedCash, note); return RedirectToAction("Index"); }
    [HttpPost] public async Task<IActionResult> Sale(PosSaleInput input)
    {
        if (!ModelState.IsValid || input.SkuIds.Length != input.Quantities.Length) { TempData["Error"] = "กรุณาตรวจข้อมูลการขาย"; return RedirectToAction("Index"); }
        var seller = await members.RequireAsync(actor.Id);
        var buyer = string.IsNullOrWhiteSpace(input.BuyerCode) ? null : await db.Members.SingleOrDefaultAsync(x => x.Code == input.BuyerCode && x.Status == MemberStatus.Active) ?? throw new BusinessException("ไม่พบรหัสสมาชิกผู้ซื้อ");
        var lines = input.SkuIds.Zip(input.Quantities, (id, qty) => new SaleLine(id, qty)).Where(x => x.Quantity > 0).ToArray();
        var order = await commerce.CheckoutAsync(input, lines, buyer?.Id, seller.Id, SalesChannel.POS, actor.Id, input.SessionId, input.Tendered, input.PaymentMethod);
        return RedirectToAction("Detail", "Orders", new { id = order.PublicId });
    }
}
