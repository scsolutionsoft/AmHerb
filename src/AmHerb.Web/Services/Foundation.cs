using System.Security.Claims;
using System.Data;
using AmHerb.Web.Data;
using AmHerb.Web.Domain;
using Microsoft.EntityFrameworkCore;

namespace AmHerb.Web.Services;

public class BusinessException(string message) : Exception(message);
public static class Money { public static decimal Round(decimal n) => decimal.Round(n, 2, MidpointRounding.AwayFromZero); }
public static class Bangkok
{
    private static readonly TimeZoneInfo Zone = ResolveZone();
    private static TimeZoneInfo ResolveZone()
    {
        // Windows NLS cannot map an IANA identifier without ICU. Use the native identifier.
        var id = OperatingSystem.IsWindows() ? "SE Asia Standard Time" : "Asia/Bangkok";
        try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
        catch (TimeZoneNotFoundException) { return FixedBangkokZone(); }
        catch (InvalidTimeZoneException) { return FixedBangkokZone(); }
    }
    // Modern Bangkok business dates are UTC+07:00 with no daylight-saving transitions.
    private static TimeZoneInfo FixedBangkokZone() => TimeZoneInfo.CreateCustomTimeZone("AMHERB.Bangkok", TimeSpan.FromHours(7), "Bangkok", "Bangkok");
    public static string Format(DateTime? utc) => utc == null ? "—" : TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc.Value, DateTimeKind.Utc), Zone).ToString("dd/MM/yyyy HH:mm", System.Globalization.CultureInfo.InvariantCulture);
}
public class Actor(IHttpContextAccessor accessor)
{
    public string Id => accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
}
public class AuditService(AmHerbDbContext db, Actor actor)
{
    public void Add(string action, object subject, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason)) throw new BusinessException("กรุณาระบุเหตุผล");
        db.AuditLogs.Add(new AuditLog { ActorId = actor.Id, Action = action, Subject = subject.ToString() ?? "", Detail = reason });
    }
}
public interface INotificationService { Task AddAsync(long? memberId, string key, string message, string? link = null); }
public class NotificationService(AmHerbDbContext db) : INotificationService
{
    public async Task AddAsync(long? memberId, string key, string message, string? link = null)
    {
        if (await db.Notifications.AnyAsync(x => x.EventKey == key)) return;
        string? userId = memberId == null ? null : await db.Members.Where(x => x.Id == memberId).Select(x => x.UserId).SingleAsync();
        db.Notifications.Add(new Notification { UserId = userId, EventKey = key, Message = message, Link = link ?? "" });
    }
}
public class MemberService(AmHerbDbContext db, AuditService audit, INotificationService notifications)
{
    public async Task<Member> RequireAsync(string userId) => await db.Members.SingleOrDefaultAsync(x => x.UserId == userId && x.Status == MemberStatus.Active) ?? throw new BusinessException("บัญชีสมาชิกไม่พร้อมใช้งาน");
    public async Task<Member> CreateAsync(string userId, string name, string? sponsorCode)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var existing = await db.Members.SingleOrDefaultAsync(x => x.UserId == userId);
        if (existing != null) return existing;
        var sponsor = string.IsNullOrWhiteSpace(sponsorCode) ? null : await db.Members.SingleOrDefaultAsync(x => x.ReferralCode == sponsorCode && x.Status == MemberStatus.Active) ?? throw new BusinessException("ไม่พบผู้แนะนำที่ใช้งานได้");
        var key = Guid.NewGuid().ToString("N");
        var member = new Member { UserId = userId, Name = name, Code = "AM" + key[..16].ToUpperInvariant(), ReferralCode = key[..20], SponsorMemberId = sponsor?.Id };
        db.Members.Add(member);
        await db.SaveChangesAsync();
        db.MemberClosures.Add(new MemberClosure { AncestorMemberId = member.Id, DescendantMemberId = member.Id, Depth = 0 });
        if (sponsor != null)
        {
            var path = await db.MemberClosures.Where(x => x.DescendantMemberId == sponsor.Id).ToListAsync();
            db.MemberClosures.AddRange(path.Select(x => new MemberClosure { AncestorMemberId = x.AncestorMemberId, DescendantMemberId = member.Id, Depth = x.Depth + 1 }));
            await notifications.AddAsync(sponsor.Id, "member:" + member.Id, "มีสมาชิกผู้แนะนำโดยตรงใหม่: " + member.Code);
        }
        // A register belongs to exactly one identity. Inventory comes from its assigned warehouse.
        var personalWarehouse = new Warehouse { Name = "คลัง " + member.Code, Kind = WarehouseKind.Member, OwnerMemberId = member.Id };
        db.Warehouses.Add(personalWarehouse);
        db.PosRegisters.Add(new PosRegister { UserId = userId, Warehouse = personalWarehouse, Name = "POS " + member.Code });
        await db.SaveChangesAsync(); await tx.CommitAsync(); return member;
    }
    public static void ValidateMove(long memberId, long? sponsorId, IEnumerable<long> descendants)
    { if (sponsorId == memberId || (sponsorId.HasValue && descendants.Contains(sponsorId.Value))) throw new BusinessException("ไม่สามารถสร้างวงจรในเครือข่ายสมาชิก"); }
    public async Task MoveAsync(long memberId, long? sponsorId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason)) throw new BusinessException("กรุณาระบุเหตุผลเปลี่ยนผู้แนะนำ");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var member = await db.Members.SingleAsync(x => x.Id == memberId);
        var subtree = await db.MemberClosures.Where(x => x.AncestorMemberId == memberId).ToListAsync();
        ValidateMove(memberId, sponsorId, subtree.Select(x => x.DescendantMemberId));
        if (sponsorId != null && !await db.Members.AnyAsync(x => x.Id == sponsorId && x.Status == MemberStatus.Active)) throw new BusinessException("ผู้แนะนำไม่พร้อมใช้งาน");
        var ids = subtree.Select(x => x.DescendantMemberId).ToArray();
        var old = await db.MemberClosures.Where(x => ids.Contains(x.DescendantMemberId) && !ids.Contains(x.AncestorMemberId)).ToListAsync();
        db.MemberClosures.RemoveRange(old);
        await db.SaveChangesAsync();
        if (sponsorId != null)
        {
            var ancestors = await db.MemberClosures.Where(x => x.DescendantMemberId == sponsorId).ToListAsync();
            foreach (var a in ancestors) foreach (var d in subtree)
                db.MemberClosures.Add(new MemberClosure { AncestorMemberId = a.AncestorMemberId, DescendantMemberId = d.DescendantMemberId, Depth = a.Depth + d.Depth + 1 });
        }
        audit.Add("Member.SponsorChanged", memberId, $"{member.SponsorMemberId} -> {sponsorId}; {reason}");
        member.SponsorMemberId = sponsorId;
        await db.SaveChangesAsync(); await tx.CommitAsync();
    }
}
public class PricingService(AmHerbDbContext db, TimeProvider clock)
{
    public async Task<ProductPrice> PriceAsync(long sku, SalesChannel channel, string tier = "Retail", DateTime? at = null)
    {
        var now = at ?? clock.GetUtcNow().UtcDateTime;
        return await db.ProductPrices.Where(x => x.SkuId == sku && x.Tier == tier && (x.Channel == null || x.Channel == channel) && x.EffectiveFrom <= now && (x.EffectiveTo == null || x.EffectiveTo > now))
            .OrderByDescending(x => x.Channel.HasValue).ThenByDescending(x => x.EffectiveFrom).ThenByDescending(x => x.Id).FirstOrDefaultAsync() ?? throw new BusinessException("ไม่พบราคาที่มีผลสำหรับสินค้า");
    }
    public async Task<TokenRate> RateAsync(long sku, SalesChannel channel, DateTime? at = null)
    {
        var now = at ?? clock.GetUtcNow().UtcDateTime;
        return await db.TokenRates.Where(x => x.SkuId == sku && (x.Channel == null || x.Channel == channel) && x.EffectiveFrom <= now && (x.EffectiveTo == null || x.EffectiveTo > now))
            .OrderByDescending(x => x.Channel.HasValue).ThenByDescending(x => x.EffectiveFrom).ThenByDescending(x => x.Id).FirstOrDefaultAsync() ?? throw new BusinessException("ไม่พบอัตรา Token");
    }
    public Task<TokenPolicy> PolicyAsync(DateTime? at = null)
    { var now = at ?? clock.GetUtcNow().UtcDateTime; return db.TokenPolicies.Where(x => x.EffectiveFrom <= now).OrderByDescending(x => x.EffectiveFrom).FirstAsync(); }
}
