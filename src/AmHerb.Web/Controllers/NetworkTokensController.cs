using System.Text;
using AmHerb.Web.Data;
using AmHerb.Web.Domain;
using AmHerb.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace AmHerb.Web.Controllers;

[Authorize]
public class NetworkTokensController(AmHerbDbContext db, MemberService members, RewardService rewards, Actor actor) : Controller
{
    private async Task<NetworkTokensPage> Report(int? level, int page)
    {
        if (level is < 0 or > 3) throw new BusinessException("เลือกระดับรางวัล 0 ถึง 3");
        var member = await members.RequireAsync(actor.Id); page = Math.Clamp(page, 1, 100000);
        var own = db.TokenDistributions.AsNoTracking().Where(x => x.MemberId == member.Id);
        var summary = await own.GroupBy(x => x.Level).Select(g => new NetworkRewardTotal(g.Key, g.Sum(x => x.Amount), g.Sum(x => x.ReversedAmount), g.Sum(x => x.ExpiredAmount))).ToListAsync();
        var rows = await own.Where(x => level == null || x.Level == level).OrderByDescending(x => x.Id).Skip((page - 1) * 30).Take(31)
            .Select(x => new NetworkRewardRow(x.Id, x.SourceOrder.Number, x.SourceOrder.Seller == null ? "" : x.SourceOrder.Seller.Code, x.Level, x.Amount, x.ReversedAmount, x.ExpiredAmount, x.Status, x.ReleaseAt, x.ExpireAt)).ToListAsync();
        var relations = await db.MemberClosures.AsNoTracking().Where(x => x.Depth > 0 && (x.DescendantMemberId == member.Id || x.AncestorMemberId == member.Id)).OrderBy(x => x.Depth).ThenBy(x => x.DescendantMemberId)
            .Select(x => new NetworkRelation(x.DescendantMemberId == member.Id ? "อัพไลน์" : "ดาวน์ไลน์", x.Depth, x.DescendantMemberId == member.Id ? x.Ancestor.Code : x.Descendant.Code, x.DescendantMemberId == member.Id ? x.Ancestor.Name : x.Descendant.Name)).Take(100).ToListAsync();
        var ledger = await db.TokenLedger.AsNoTracking().Where(x => x.MemberId == member.Id).OrderByDescending(x => x.Id).Take(50).ToListAsync();
        return new NetworkTokensPage(await rewards.BalanceAsync(member.Id), summary, rows.Take(30).ToList(), relations, ledger, level, page, rows.Count > 30);
    }
    public async Task<IActionResult> Index(int? level, int page = 1) => View(await Report(level, page));
    public async Task<IActionResult> Export(int? level)
    {
        if (level is < 0 or > 3) throw new BusinessException("ระดับไม่ถูกต้อง");
        var member = await members.RequireAsync(actor.Id);
        var rows = await db.TokenDistributions.AsNoTracking().Where(x => x.MemberId == member.Id && (level == null || x.Level == level)).OrderByDescending(x => x.Id).Take(10000)
            .Select(x => new { x.SourceOrder.Number, x.Level, x.Amount, x.ReversedAmount, x.ExpiredAmount, x.ReleaseAt, x.ExpireAt }).ToListAsync();
        static string N(decimal value) => value.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
        var lines = new List<string[]> { new[] { "คำสั่งซื้อ", "ระดับ", "รางวัลเดิม", "ปรับคืน", "หมดอายุ", "ปลดล็อก", "วันหมดอายุ" } };
        lines.AddRange(rows.Select(x => new[] { x.Number, x.Level.ToString(), N(x.Amount), N(x.ReversedAmount), N(x.ExpiredAmount), x.ReleaseAt.AddHours(7).ToString("yyyy-MM-dd HH:mm"), x.ExpireAt.AddHours(7).ToString("yyyy-MM-dd HH:mm") }));
        var csv = string.Join("\r\n", lines.Select(x => string.Join(",", x.Select(DashboardController.CsvCell))));
        return File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray(), "text/csv; charset=utf-8", "my-network-rewards.csv");
    }
}
public record NetworkRewardTotal(int Level, decimal Original, decimal Reversed, decimal Expired);
public record NetworkRewardRow(long Id, string Order, string SellerCode, int Level, decimal Amount, decimal Reversed, decimal Expired, TokenStatus Status, DateTime ReleaseAt, DateTime ExpireAt);
public record NetworkRelation(string Direction, int Depth, string Code, string Name);
public record NetworkTokensPage(WalletBalance Wallet, List<NetworkRewardTotal> Summary, List<NetworkRewardRow> Rewards, List<NetworkRelation> Relations, List<TokenLedger> Ledger, int? Level, int Page, bool HasNext);
