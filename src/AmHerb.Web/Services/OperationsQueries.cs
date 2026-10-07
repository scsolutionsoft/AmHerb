using AmHerb.Web.Data;
using AmHerb.Web.Domain;
using AmHerb.Web.Models;
using Microsoft.EntityFrameworkCore;
namespace AmHerb.Web.Services;
public class OperationsQueries(AmHerbDbContext db)
{
    // Classify the selling operation independently of referral/reward ownership.
    // POS uses the warehouses of its allocated lots, not the register's current warehouse.
    public static IQueryable<Order> Origin(IQueryable<Order> orders, string origin) => origin switch
    {
        "store" => orders.Where(x => x.StoreId != null),
        "member-pos" => orders.Where(x => x.StoreId == null && x.Channel == SalesChannel.POS && x.Items.Any(i => i.Allocations.Any(a => a.InventoryBatch.Warehouse.OwnerMemberId != null))),
        "central-pos" => orders.Where(x => x.StoreId == null && x.Channel == SalesChannel.POS && !x.Items.Any(i => i.Allocations.Any(a => a.InventoryBatch.Warehouse.OwnerMemberId != null))),
        "marketplace" => orders.Where(x => x.StoreId == null && (x.Channel == SalesChannel.Shopee || x.Channel == SalesChannel.TikTokShop || x.Channel == SalesChannel.Lazada)),
        "central" => orders.Where(x => x.StoreId == null && (x.Channel == SalesChannel.Store || x.Channel == SalesChannel.Manual || (x.Channel == SalesChannel.POS && !x.Items.Any(i => i.Allocations.Any(a => a.InventoryBatch.Warehouse.OwnerMemberId != null))))),
        _ => orders
    };
    public static string OriginLabel(Order o) => o.StoreId != null ? "ร้านออนไลน์สมาชิก" : o.Channel == SalesChannel.POS
        ? (o.Items.Any(i => i.Allocations.Any(a => a.InventoryBatch.Warehouse.OwnerMemberId != null)) ? "POS คลังสมาชิก" : "POS คลังส่วนกลาง")
        : o.Channel is SalesChannel.Shopee or SalesChannel.TikTokShop or SalesChannel.Lazada ? "Marketplace" : "ส่วนกลาง / แอดมิน";
    public async Task<OperationsOrders> Orders(OperationsOrders m)
    {
        if (!OperationsOrders.Origins.ContainsKey(m.Origin) || !OperationsOrders.Relations.ContainsKey(m.Relation)) throw new BusinessException("ตัวกรองไม่ถูกต้อง");
        if (m.To < m.From || m.From.Year < 1900 || m.To.Year > 9998) throw new BusinessException("ช่วงวันที่ไม่ถูกต้อง");
        m.Page = Math.Clamp(m.Page, 1, 100000); var start = m.From.Date.AddHours(-7); var end = m.To.Date.AddDays(1).AddHours(-7);
        var query = Origin(db.Orders.AsNoTracking().Where(x => x.CreatedAt >= start && x.CreatedAt < end), m.Origin);
        if (!string.IsNullOrWhiteSpace(m.MemberCode))
        {
            m.Member = await db.Members.AsNoTracking().SingleOrDefaultAsync(x => x.Code == m.MemberCode.Trim()) ?? throw new BusinessException("ไม่พบรหัสสมาชิก");
            var member = m.Member;
            if (m.Relation == "buyer") query = query.Where(x => x.BuyerMemberId == member.Id);
            else if (m.Relation == "reward") query = query.Where(x => db.TokenDistributions.Any(d => d.SourceOrderId == x.Id && d.MemberId == member.Id));
            else
            {
                var ids = m.Relation == "downline" ? db.MemberClosures.Where(x => x.AncestorMemberId == member.Id && x.Depth > 0).Select(x => x.DescendantMemberId)
                    : m.Relation == "upline" ? db.MemberClosures.Where(x => x.DescendantMemberId == member.Id && x.Depth > 0).Select(x => x.AncestorMemberId)
                    : db.Members.Where(x => x.Id == member.Id).Select(x => x.Id);
                query = query.Where(x => (x.SellerMemberId != null && ids.Contains(x.SellerMemberId.Value)) || (x.Store != null && ids.Contains(x.Store.MemberId)) || (x.Channel == SalesChannel.POS && db.Members.Any(a => ids.Contains(a.Id) && a.UserId == x.CashierUserId)));
            }
        }
        else if (m.Relation != "sales") throw new BusinessException("ระบุรหัสสมาชิกก่อนเลือกความสัมพันธ์");
        if (!string.IsNullOrWhiteSpace(m.Q)) query = query.Where(x => x.Number.Contains(m.Q) || x.CustomerName.Contains(m.Q) || x.StoreName.Contains(m.Q));
        if (m.Status != null) query = query.Where(x => x.Status == m.Status);
        if (!string.IsNullOrWhiteSpace(m.Payment)) query = query.Where(x => db.Payments.Any(p => p.OrderId == x.Id && p.Status == m.Payment));
        if (m.Shipping) query = query.Where(x => x.Channel != SalesChannel.POS && (x.Status == OrderStatus.Paid || x.Status == OrderStatus.Processing || x.Status == OrderStatus.Shipped));
        m.Total = await query.CountAsync(); m.Payable = await query.SumAsync(x => (decimal?)x.CashPayable) ?? 0;
        m.Orders = await query.Include(x => x.Buyer).Include(x => x.Seller).Include(x => x.Store).ThenInclude(x => x!.Member)
            .Include(x => x.Items).ThenInclude(x => x.Allocations).ThenInclude(x => x.InventoryBatch).ThenInclude(x => x.Warehouse)
            .AsSplitQuery().OrderByDescending(x => x.Id).Skip((m.Page - 1) * 50).Take(50).ToListAsync();
        var orderIds = m.Orders.Select(x => x.Id).ToArray();
        m.Payments = await db.Payments.AsNoTracking().Where(x => orderIds.Contains(x.OrderId)).ToDictionaryAsync(x => x.OrderId);
        m.Shipments = await db.Shipments.AsNoTracking().Where(x => orderIds.Contains(x.OrderId)).ToListAsync();
        var cashiers = m.Orders.Select(x => x.CashierUserId).ToArray();
        m.Cashiers = await db.Members.AsNoTracking().Where(x => cashiers.Contains(x.UserId)).ToDictionaryAsync(x => x.UserId);
        return m;
    }
}
