using System.Data;
using AmHerb.Web.Data;
using AmHerb.Web.Domain;
using Microsoft.EntityFrameworkCore;
namespace AmHerb.Web.Services;

public class MemberStoreService(AmHerbDbContext db, MemberService members, AuditService audit)
{
    public async Task<MemberStore> RequireAsync(string userId) => await db.MemberStores.Include(x => x.Warehouse)
        .SingleOrDefaultAsync(x => x.Member.UserId == userId && x.Member.Status == MemberStatus.Active && x.Warehouse.OwnerMemberId == x.MemberId)
        ?? throw new BusinessException("กรุณาสร้างร้านของคุณก่อน");

    public async Task<MemberStore> CreateAsync(string userId)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var member = await members.RequireAsync(userId);
        var prior = await db.MemberStores.SingleOrDefaultAsync(x => x.MemberId == member.Id);
        if (prior != null) return prior;
        var warehouse = await db.Warehouses.Where(x => x.OwnerMemberId == member.Id && x.Active).OrderBy(x => x.Id).FirstOrDefaultAsync()
            ?? throw new BusinessException("ยังไม่มีคลังสมาชิกที่ใช้งานได้ กรุณาติดต่อผู้ดูแล");
        var store = new MemberStore { MemberId = member.Id, WarehouseId = warehouse.Id, Name = member.Name, Slug = member.Code.ToLowerInvariant(), Phone = member.Phone };
        db.MemberStores.Add(store); await db.SaveChangesAsync(); audit.Add("Store.Create", store.Id, "สร้างร้านสมาชิกแบบร่าง");
        await db.SaveChangesAsync(); await tx.CommitAsync(); return store;
    }

    public async Task SaveAsync(string userId, string name, string description, string phone, bool published)
    {
        var store = await RequireAsync(userId);
        if (string.IsNullOrWhiteSpace(name) || name.Length > 160 || description.Length > 2000 || phone.Length > 40 || (published && string.IsNullOrWhiteSpace(phone))) throw new BusinessException("ตรวจสอบชื่อร้าน รายละเอียด และเบอร์ติดต่อก่อนเปิดร้าน");
        if (published && (!store.Warehouse.Active || store.Warehouse.OwnerMemberId != store.MemberId)) throw new BusinessException("คลังร้านไม่พร้อมใช้งาน");
        store.Name = name.Trim(); store.Description = description.Trim(); store.Phone = phone.Trim(); store.Published = published;
        audit.Add("Store.Settings", store.Id, published ? "เปิดร้าน" : "พักร้าน"); await db.SaveChangesAsync();
    }

    public async Task SetProductAsync(string userId, long skuId, bool enabled)
    {
        var store = await RequireAsync(userId);
        if (!await db.Skus.AnyAsync(x => x.Id == skuId && x.Active && x.Product.Active)) throw new BusinessException("ไม่พบสินค้า");
        var product = await db.StoreProducts.SingleOrDefaultAsync(x => x.StoreId == store.Id && x.SkuId == skuId);
        if (product == null) { product = new StoreProduct { StoreId = store.Id, SkuId = skuId }; db.StoreProducts.Add(product); }
        product.Enabled = enabled; audit.Add("Store.Product", store.Id, $"SKU {skuId}; enabled={enabled}"); await db.SaveChangesAsync();
    }

    public async Task<Order> RequireOrderAsync(string userId, long id)
    {
        var store = await RequireAsync(userId);
        return await db.Orders.SingleOrDefaultAsync(x => x.Id == id && (x.StoreId == store.Id || (x.Channel == SalesChannel.POS && x.CashierUserId == userId)))
            ?? throw new BusinessException("ไม่พบคำสั่งซื้อของร้านคุณ");
    }

    public async Task AddExpenseAsync(string userId, Guid key, DateTime day, decimal amount, string description, string reference)
    {
        var store = await RequireAsync(userId);
        if (key == Guid.Empty || amount <= 0 || amount > 10000000 || Money.Round(amount) != amount || string.IsNullOrWhiteSpace(description) || description.Length > 200 || reference.Length > 200 || day.Date > DateTime.UtcNow.AddHours(7).Date || day.Year < 2000) throw new BusinessException("วันที่ ยอดเงิน หรือรายละเอียดค่าใช้จ่ายไม่ถูกต้อง");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var prior = await db.StoreExpenses.SingleOrDefaultAsync(x => x.StoreId == store.Id && x.RequestKey == key);
        if (prior != null) { if (prior.Amount != amount || prior.Description != description.Trim() || prior.OccurredAt != day.Date.AddHours(-7) || prior.Reference != reference.Trim()) throw new BusinessException("รหัสค่าใช้จ่ายถูกใช้แล้ว"); return; }
        db.StoreExpenses.Add(new StoreExpense { StoreId = store.Id, RequestKey = key, OccurredAt = day.Date.AddHours(-7), Amount = amount, Description = description.Trim(), Reference = reference.Trim() });
        audit.Add("Store.Expense", store.Id, description); await db.SaveChangesAsync(); await tx.CommitAsync();
    }

    public async Task ReverseExpenseAsync(string userId, long id, string reason)
    {
        var store = await RequireAsync(userId);
        if (string.IsNullOrWhiteSpace(reason) || reason.Length > 160) throw new BusinessException("ระบุเหตุผลยกเลิกค่าใช้จ่าย");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var original = await db.StoreExpenses.SingleOrDefaultAsync(x => x.Id == id && x.StoreId == store.Id && x.ReversesId == null && x.Amount > 0) ?? throw new BusinessException("ไม่พบค่าใช้จ่าย");
        if (await db.StoreExpenses.AnyAsync(x => x.ReversesId == id)) return;
        db.StoreExpenses.Add(new StoreExpense { StoreId = store.Id, RequestKey = Guid.NewGuid(), OccurredAt = DateTime.UtcNow, Amount = -original.Amount, Description = "ยกเลิก: " + reason, Reference = original.Reference, ReversesId = id });
        audit.Add("Store.ExpenseReverse", id, reason); await db.SaveChangesAsync(); await tx.CommitAsync();
    }
}
