using AmHerb.Web.Data;
using AmHerb.Web.Domain;
using AmHerb.Web.Models;
using AmHerb.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace AmHerb.Web.Controllers;
[Authorize]
public class CreditController(AmHerbDbContext db,CreditService credit,Actor actor,TimeProvider clock,IAuthorizationService authorization,IConfiguration configuration):Controller
{
    private async Task<bool> Finance()=> (await authorization.AuthorizeAsync(User,"Payments")).Succeeded;
    private async Task<CreditAccount?> Visible(long id)=>await db.CreditAccounts.AsNoTracking().Include(x=>x.Member).SingleOrDefaultAsync(x=>x.Id==id&&((User.IsInRole("Finance")||User.IsInRole("Admin")||User.IsInRole("SuperAdmin"))||x.Member.UserId==actor.Id));
    public async Task<IActionResult> Index(string? q,int page=1)
    {
        var finance=await Finance();var now=clock.GetUtcNow().UtcDateTime;page=Math.Max(1,page);
        var accounts=db.CreditAccounts.AsNoTracking().Where(x=>finance||x.Member.UserId==actor.Id);
        if(!string.IsNullOrWhiteSpace(q))accounts=accounts.Where(x=>x.Member.Code.Contains(q)||x.Member.Name.Contains(q));
        var invoices=db.CreditInvoices.Where(i=>accounts.Any(a=>a.Id==i.CreditAccountId));
        var rows=accounts.OrderBy(x=>x.Member.Code).Skip((page-1)*50).Take(50).Select(x=>new CreditRow(x.Id,x.MemberId,x.Member.Code,x.Member.Name,x.MasterDealer,x.Enabled,x.Limit,x.TermDays,
            db.CreditInvoices.Where(i=>i.CreditAccountId==x.Id).Sum(i=>(decimal?)(i.Amount-i.Paid-i.Adjusted))??0,
            db.CreditInvoices.Where(i=>i.CreditAccountId==x.Id&&i.DueAt<now).Sum(i=>(decimal?)(i.Amount-i.Paid-i.Adjusted))??0,
            db.CreditReceipts.Count(r=>r.CreditAccountId==x.Id&&r.Status==CreditReceiptStatus.Pending)));
        return View(new CreditListModel{Finance=finance,Search=q,Page=page,Total=await accounts.CountAsync(),Used=await invoices.SumAsync(x=>x.Amount-x.Paid-x.Adjusted),Overdue=await invoices.Where(x=>x.DueAt<now).SumAsync(x=>x.Amount-x.Paid-x.Adjusted),Rows=await rows.ToListAsync()});
    }
    public async Task<IActionResult> Account(long id)
    {
        var account=await Visible(id);if(account==null)return NotFound();
        ViewBag.BankInstructions=configuration["Payments:BankInstructions"]??"ติดต่อ AM HERB เพื่อขอข้อมูลบัญชีธนาคาร และแจ้งรหัสสมาชิกเมื่อโอนชำระ";
        return View(new CreditAccountModel{Account=account,Finance=await Finance(),Now=clock.GetUtcNow().UtcDateTime,
            Invoices=await db.CreditInvoices.AsNoTracking().Include(x=>x.Order).Where(x=>x.CreditAccountId==id).OrderByDescending(x=>x.IssuedAt).ToListAsync(),
            Receipts=await db.CreditReceipts.AsNoTracking().Where(x=>x.CreditAccountId==id).OrderByDescending(x=>x.Id).Select(x=>new ReceiptRow(x.Id,x.Amount,x.TransferredAt,x.Reference,x.Status,x.Note,x.ReviewNote,x.Slip!=null)).ToListAsync()});
    }
    [HttpPost,Authorize(Policy="Payments")]
    public async Task<IActionResult> Configure(string code,decimal limit,int days,bool enabled,bool master,string reason,string? version)
    {
        var member=await db.Members.SingleOrDefaultAsync(x=>x.Code==code)??throw new BusinessException("ไม่พบรหัสสมาชิก");
        await credit.Configure(member.Id,limit,days,enabled,master,reason,version);
        TempData["Success"]="บันทึกสิทธิ์และวงเงินเครดิตแล้ว";
        return RedirectToAction(nameof(Account),new{id=await db.CreditAccounts.Where(x=>x.MemberId==member.Id).Select(x=>x.Id).SingleAsync()});
    }
    [HttpPost,RequestSizeLimit(6*1024*1024)]
    public async Task<IActionResult> Submit(long accountId,Guid key,decimal amount,DateTime transferred,string reference,string? note,IFormFile? slip)
    {
        if(await Visible(accountId)==null)return NotFound();
        await credit.Submit(accountId,actor.Id,await Finance(),key,amount,DateTime.SpecifyKind(transferred,DateTimeKind.Unspecified).AddHours(-7),reference??"",note,slip==null?null:await MediaService.Read(slip));
        TempData["Success"]="รับแจ้งโอนแล้ว วงเงินจะคืนเมื่อฝ่ายการเงินยืนยัน";return RedirectToAction(nameof(Account),new{id=accountId});
    }
    [HttpPost,Authorize(Policy="Payments")]
    public async Task<IActionResult> Review(long id,bool approve,string reason)
    {
        var accountId=await db.CreditReceipts.Where(x=>x.Id==id).Select(x=>(long?)x.CreditAccountId).SingleOrDefaultAsync();if(accountId==null)return NotFound();
        await credit.Review(id,approve,actor.Id,reason);TempData["Success"]=approve?"ยืนยันยอดโอน ตัดหนี้ และคืนวงเงินแล้ว":"ปฏิเสธรายการโอนแล้ว";
        return RedirectToAction(nameof(Account),new{id=accountId});
    }
    public async Task<IActionResult> Slip(long id)
    {
        var account=await db.CreditReceipts.Where(x=>x.Id==id).Select(x=>(long?)x.CreditAccountId).SingleOrDefaultAsync();
        if(account==null||await Visible(account.Value)==null)return NotFound();
        var receipt=await db.CreditReceipts.AsNoTracking().SingleAsync(x=>x.Id==id);if(receipt.Slip==null)return NotFound();
        Response.Headers.CacheControl="private, no-store";return File(receipt.Slip,receipt.SlipType);
    }
    public async Task<IActionResult> Receipt(long id)
    {
        var receipt=await db.CreditReceipts.AsNoTracking().Include(x=>x.CreditAccount).ThenInclude(x=>x.Member).SingleOrDefaultAsync(x=>x.Id==id);
        if(receipt==null||await Visible(receipt.CreditAccountId)==null)return NotFound();
        ViewBag.Allocations=await db.CreditAllocations.AsNoTracking().Include(x=>x.CreditInvoice).ThenInclude(x=>x.Order).Where(x=>x.CreditReceiptId==id).ToListAsync();return View(receipt);
    }
    public async Task<IActionResult> Statement(long id,DateTime? from,DateTime? to)
    {
        var account=await Visible(id);if(account==null)return NotFound();
        var start=(from??clock.GetUtcNow().UtcDateTime.AddHours(7).Date.AddDays(-30)).Date;
        var end=(to??clock.GetUtcNow().UtcDateTime.AddHours(7).Date).Date;
        if(end<start||end-start>TimeSpan.FromDays(366))throw new BusinessException("เลือกช่วงวันที่ไม่เกิน 366 วัน");
        var begin=start.AddHours(-7);var finish=end.AddDays(1).AddHours(-7);
        var journals=db.JournalEntries.AsNoTracking().Where(x=>db.CreditInvoices.Any(i=>i.OrderId==x.OrderId&&i.CreditAccountId==id));
        var opening=await journals.Where(x=>x.PostedAt<begin).SelectMany(x=>x.Lines).Where(x=>x.Account=="1100").SumAsync(x=>x.Debit-x.Credit);
        var entries=await journals.Include(x=>x.Lines).Include(x=>x.Order).Where(x=>x.PostedAt>=begin&&x.PostedAt<finish&&x.Lines.Any(l=>l.Account=="1100")).OrderBy(x=>x.PostedAt).ThenBy(x=>x.Id).ToListAsync();
        return View(new CreditStatement(account,start,end,opening,entries));
    }
}
