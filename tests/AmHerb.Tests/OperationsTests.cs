using AmHerb.Web.Domain;
using AmHerb.Web.Models;
using AmHerb.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
namespace AmHerb.Tests;
[Collection("SQL")]
public class OperationsTests(SqlFixture fixture)
{
    [SqlFact] public async Task Stock_adjustment_rejects_stale_versions_and_preserves_reserved_stock()
    {
        await using var h = new Harness(fixture); await h.InitializeAsync();
        var batch = await h.Db.InventoryBatches.SingleAsync(x => x.SkuId == h.SkuId);
        batch.QtyAvailable = 97; batch.QtyReserved = 3; await h.Db.SaveChangesAsync();
        var version = Convert.ToBase64String(batch.RowVersion);
        await h.Inventory.AdjustAsync(batch.Id, 2, InventoryKind.Adjust, "Count correction", version);
        await Assert.ThrowsAsync<BusinessException>(() => h.Inventory.AdjustAsync(batch.Id, 2, InventoryKind.Adjust, "Duplicate old screen", version));
        Assert.Equal(99, batch.QtyAvailable); Assert.Equal(3, batch.QtyReserved);
        await Assert.ThrowsAsync<BusinessException>(() => h.Inventory.AdjustAsync(batch.Id, -103, InventoryKind.Adjust, "Too many"));
        await Assert.ThrowsAsync<BusinessException>(() => h.Inventory.AdjustAsync(batch.Id, 1, InventoryKind.Damage, "Cannot increase damage"));
        await Assert.ThrowsAsync<BusinessException>(() => h.Inventory.AdjustAsync(batch.Id, 1, InventoryKind.Adjust, ""));
        Assert.Single(await h.Db.InventoryTransactions.Where(x => x.InventoryBatchId == batch.Id && x.Kind == InventoryKind.Adjust).ToListAsync());
    }
    [SqlFact] public async Task Origin_and_member_relationships_do_not_mix_referrals_with_store_ownership()
    {
        await using var h = new Harness(fixture); await h.InitializeAsync();
        var parent = await h.MemberAsync(); var seller = await h.MemberAsync(parent); var buyer = await h.MemberAsync();
        var stores = new MemberStoreService(h.Db, h.Members, new AuditService(h.Db, new Actor(new HttpContextAccessor())));
        var store = await stores.CreateAsync(seller.UserId);
        var day = DateTime.UtcNow.Date;
        Order New(string suffix, SalesChannel channel, long? storeId = null) => new() { Number = Guid.NewGuid().ToString("N") + suffix, IdempotencyKey = Guid.NewGuid().ToString(), Channel = channel, StoreId = storeId, SellerMemberId = seller.Id, BuyerMemberId = buyer.Id, CustomerName = "Operations test", CreatedAt = day, CashPayable = 100 };
        var central = New("C", SalesChannel.Store); var owned = New("S", SalesChannel.Store, store.Id); var market = New("M", SalesChannel.Shopee);
        h.Db.Orders.AddRange(central, owned, market); await h.Db.SaveChangesAsync();
        var queries = new OperationsQueries(h.Db);
        OperationsOrders Filter(string origin = "all", string relation = "sales", string? code = null) => new() { Origin = origin, Relation = relation, MemberCode = code ?? seller.Code, From = day.AddDays(-1), To = day.AddDays(1) };
        Assert.Equal(central.Id, Assert.Single((await queries.Orders(Filter("central"))).Orders).Id);
        Assert.Equal(owned.Id, Assert.Single((await queries.Orders(Filter("store"))).Orders).Id);
        Assert.Equal(market.Id, Assert.Single((await queries.Orders(Filter("marketplace"))).Orders).Id);
        var all = await queries.Orders(Filter()); Assert.Equal(3, all.Total); Assert.Equal(300, all.Payable);
        Assert.Equal(3, (await queries.Orders(Filter(relation: "downline", code: parent.Code))).Total);
        Assert.Equal(3, (await queries.Orders(Filter(relation: "buyer", code: buyer.Code))).Total);
        Assert.Equal(0, (await queries.Orders(Filter(relation: "sales", code: buyer.Code))).Total);
        var item = new OrderItem { OrderId = central.Id, SkuId = h.SkuId, Name = "historical reward", Quantity = 1 }; h.Db.OrderItems.Add(item); await h.Db.SaveChangesAsync();
        h.Db.TokenDistributions.Add(new TokenDistribution { MemberId = parent.Id, OrderItemId = item.Id, SourceOrderId = central.Id, Level = 1, Amount = 1 }); await h.Db.SaveChangesAsync();
        await h.Members.MoveAsync(seller.Id, null, "Verify historical source remains after moving network");
        Assert.Equal(0, (await queries.Orders(Filter(relation: "downline", code: parent.Code))).Total);
        Assert.Equal(central.Id, Assert.Single((await queries.Orders(Filter(relation: "reward", code: parent.Code))).Orders).Id);
    }
    [SqlFact] public async Task POS_classification_uses_allocated_warehouse_and_not_current_register()
    {
        await using var h = new Harness(fixture); await h.InitializeAsync(); var member = await h.MemberAsync();
        var order = await h.SellAsync(member);
        var register = await h.Db.PosRegisters.SingleAsync(x => x.UserId == member.UserId);
        register.WarehouseId = (await h.Db.Warehouses.SingleAsync(x => x.OwnerMemberId == member.Id)).Id;
        await h.Db.SaveChangesAsync();
        Assert.True(await OperationsQueries.Origin(h.Db.Orders.Where(x => x.Id == order.Id), "central-pos").AnyAsync());
        Assert.False(await OperationsQueries.Origin(h.Db.Orders.Where(x => x.Id == order.Id), "member-pos").AnyAsync());
    }
}
