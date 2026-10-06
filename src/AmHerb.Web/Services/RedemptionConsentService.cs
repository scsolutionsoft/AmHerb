using System.Security.Cryptography;
using System.Text;
using AmHerb.Web.Data;
using AmHerb.Web.Domain;
using Microsoft.EntityFrameworkCore;

namespace AmHerb.Web.Services;
public class RedemptionConsentService(AmHerbDbContext db, RewardService rewards, TimeProvider clock)
{
    public static string Hash(string code) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code.Trim())));
    public async Task<string> IssueAsync(long memberId, long registerId, decimal maximumTokens)
    {
        if (maximumTokens <= 0 || Money.Round(maximumTokens) != maximumTokens || maximumTokens > (await rewards.BalanceAsync(memberId)).Available) throw new BusinessException("ยอดอนุมัติเกิน Token พร้อมใช้");
        if (!await db.PosRegisters.AnyAsync(x => x.Id == registerId && x.Enabled)) throw new BusinessException("ไม่พบจุดขาย");
        var code = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        db.RedemptionAuthorizations.Add(new RedemptionAuthorization { MemberId = memberId, PosRegisterId = registerId, MaximumTokens = maximumTokens, CodeHash = Hash(code), ExpiresAt = clock.GetUtcNow().UtcDateTime.AddMinutes(5) });
        await db.SaveChangesAsync(); return code;
    }
}
