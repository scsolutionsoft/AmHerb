using System.Data;
using AmHerb.Web.Data;
using AmHerb.Web.Domain;
using Microsoft.EntityFrameworkCore;

namespace AmHerb.Web.Services;

public class PosService(AmHerbDbContext db, MemberService members, AuditService audit, TimeProvider clock)
{
    public async Task<PosRegister> RegisterAsync(string userId)
    {
        await members.RequireAsync(userId);
        return await db.PosRegisters.Include(x => x.Warehouse).SingleOrDefaultAsync(x => x.UserId == userId && x.Enabled) ?? throw new BusinessException("POS ของคุณถูกปิดใช้งาน ติดต่อผู้ดูแลระบบ");
    }
    public async Task OpenAsync(string userId, decimal opening)
    {
        if (opening < 0 || opening > 10000000 || Money.Round(opening) != opening) throw new BusinessException("เงินเปิดกะไม่ถูกต้อง");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var register = await RegisterAsync(userId);
        if (await db.PosSessions.AnyAsync(x => x.PosRegisterId == register.Id && x.ClosedAt == null)) throw new BusinessException("คุณมีกะที่ยังไม่ปิด");
        db.PosSessions.Add(new PosSession { PosRegisterId = register.Id, OpeningCash = opening, OpenedAt = clock.GetUtcNow().UtcDateTime });
        audit.Add("POS.Open", register.Id, $"Opening cash {opening}");
        await db.SaveChangesAsync(); await tx.CommitAsync();
    }
    public async Task CloseAsync(string userId, long sessionId, decimal counted, string note)
    {
        if (counted < 0 || counted > 100000000 || string.IsNullOrWhiteSpace(note)) throw new BusinessException("กรุณาระบุเงินนับจริงและหมายเหตุ");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var session = await db.PosSessions.SingleOrDefaultAsync(x => x.Id == sessionId && x.PosRegister.UserId == userId && x.ClosedAt == null) ?? throw new BusinessException("ไม่พบกะที่เปิดของคุณ");
        var cash = await db.Payments.Where(x => x.Order.PosSessionId == sessionId && x.Provider == "Cash" && x.Status == "Confirmed").SumAsync(x => (decimal?)x.Amount) ?? 0;
        // Approved refunds are processed by Finance, outside the cashier's drawer.
        session.ExpectedCash = session.OpeningCash + cash; session.CountedCash = counted; session.Difference = counted - session.ExpectedCash;
        session.ClosedAt = clock.GetUtcNow().UtcDateTime; session.Note = note;
        audit.Add("POS.Close", sessionId, $"Expected {session.ExpectedCash}; counted {counted}; {note}");
        await db.SaveChangesAsync(); await tx.CommitAsync();
    }
}
