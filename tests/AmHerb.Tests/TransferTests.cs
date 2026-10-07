using System.Security.Claims;
using AmHerb.Web.Data;
using AmHerb.Web.Domain;
using AmHerb.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace AmHerb.Tests;
[Collection("SQL")]
public class TransferTests(SqlFixture fixture)
{
    [SqlFact] public async Task Transfer_uses_only_unreserved_stock_and_sale_finalization_does_not_deduct_twice()
    {
        await using var h = new Harness(fixture); await h.InitializeAsync(10); var owner = await h.MemberAsync();
        var user = Principal(owner); var staff = Principal(owner, "Warehouse");
        var destination = await h.Db.Warehouses.SingleAsync(x => x.OwnerMemberId == owner.Id);
        var order = new Order { Number = Guid.NewGuid().ToString(), IdempotencyKey = Guid.NewGuid().ToString() };
        var item = new OrderItem { Order = order, SkuId = h.SkuId, Quantity = 7, Name = "Reserved for sale" };
        order.Items.Add(item); h.Db.Orders.Add(order); await h.Db.SaveChangesAsync();
        await h.Inventory.ReserveAsync(item, h.WarehouseId); await h.Db.SaveChangesAsync();
        var service = Service(h.Db, h.Clock, staff);
        var tooMany = await service.RequestAsync(user, Guid.NewGuid(), h.WarehouseId, destination.Id, h.SkuId, 4, "Cannot take reserved stock");
        await Assert.ThrowsAsync<BusinessException>(() => service.DispatchAsync(staff, tooMany.Id, "TEST", "Stock reserved elsewhere"));
        var transfer = await service.RequestAsync(user, Guid.NewGuid(), h.WarehouseId, destination.Id, h.SkuId, 3, "Only unreserved stock");
        await service.DispatchAsync(staff, transfer.Id, "TEST", "Transfer three");
        var batch = await h.Db.InventoryBatches.SingleAsync(x => x.WarehouseId == h.WarehouseId && x.SkuId == h.SkuId);
        Assert.Equal(0, batch.QtyAvailable); Assert.Equal(7, batch.QtyReserved);
        await h.Inventory.FinalizeAsync(order, true); await h.Db.SaveChangesAsync();
        Assert.Equal(0, batch.QtyAvailable); Assert.Equal(0, batch.QtyReserved);
        await service.ReceiveAsync(user, transfer.Id, "Received three");
        Assert.Equal(3, await h.Db.InventoryBatches.Where(x => x.WarehouseId == destination.Id).SumAsync(x => x.QtyAvailable));
    }
    private static ClaimsPrincipal Principal(Member member, string role = "Member") => new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, member.UserId), new Claim(ClaimTypes.Role, role)], "Test"));
    private static StockTransferService Service(AmHerbDbContext db, TimeProvider clock, ClaimsPrincipal user) => new(db, clock, new AuditService(db, new Actor(new HttpContextAccessor { HttpContext = new DefaultHttpContext { User = user } })), new NotificationService(db));
    [SqlFact] public async Task Transfer_FEFO_dispatch_receive_replays_preserve_total_and_do_not_issue_rewards()
    {
        await using var h = new Harness(fixture); await h.InitializeAsync(10);
        var owner = await h.MemberAsync(); var user = Principal(owner); var staff = Principal(owner, "Warehouse");
        var destination = await h.Db.Warehouses.SingleAsync(x => x.OwnerMemberId == owner.Id);
        await h.Inventory.ReceiveAsync(h.SkuId, h.WarehouseId, "FIRST", h.Clock.Now.AddDays(-5), h.Clock.Now.AddDays(30), 3, 100, "Earlier expiry");
        var service = Service(h.Db, h.Clock, user); var key = Guid.NewGuid();
        var t = await service.RequestAsync(user, key, h.WarehouseId, destination.Id, h.SkuId, 5, "Replenish");
        Assert.Equal(t.Id, (await service.RequestAsync(user, key, h.WarehouseId, destination.Id, h.SkuId, 5, "Replenish")).Id);
        Assert.Equal(13, await h.Db.InventoryBatches.Where(x => x.SkuId == h.SkuId).SumAsync(x => x.QtyAvailable));
        await service.DispatchAsync(staff, t.Id, "TRACK-1", "Approved"); await service.DispatchAsync(staff, t.Id, "TRACK-1", "Approved");
        Assert.Equal(8, await h.Db.InventoryBatches.Where(x => x.SkuId == h.SkuId).SumAsync(x => x.QtyAvailable));
        Assert.Equal(3, (await h.Db.TransferAllocations.Include(x => x.SourceBatch).SingleAsync(x => x.StockTransferId == t.Id && x.SourceBatch.LotNo == "FIRST")).Quantity);
        Assert.Equal(5, await h.Db.StockTransfers.Where(x => x.Id == t.Id && x.Status == TransferStatus.Dispatched).SumAsync(x => x.Quantity));
        Assert.False(await h.Db.InventoryBatches.AnyAsync(x => x.WarehouseId == destination.Id));
        await service.ReceiveAsync(user, t.Id, "Counted complete"); await service.ReceiveAsync(user, t.Id, "Counted complete");
        Assert.Equal(13, await h.Db.InventoryBatches.Where(x => x.SkuId == h.SkuId).SumAsync(x => x.QtyAvailable));
        Assert.Equal(5, await h.Db.InventoryBatches.Where(x => x.WarehouseId == destination.Id).SumAsync(x => x.QtyAvailable));
        Assert.Equal(4, await h.Db.InventoryTransactions.CountAsync(x => x.Reason.StartsWith($"Transfer #{t.Id};")));
        Assert.False(await h.Db.TokenDistributions.AnyAsync(x => x.OrderItem!.SkuId == h.SkuId));
        Assert.Equal(TransferStatus.Received, t.Status);
    }
    [SqlFact] public async Task Transfer_rejects_other_owners_and_unauthorized_dispatch()
    {
        await using var h = new Harness(fixture); await h.InitializeAsync(); var owner = await h.MemberAsync(); var outsider = await h.MemberAsync();
        var destination = await h.Db.Warehouses.SingleAsync(x => x.OwnerMemberId == owner.Id); var user = Principal(owner); var other = Principal(outsider); var staff = Principal(owner, "Warehouse");
        var service = Service(h.Db, h.Clock, user);
        await Assert.ThrowsAsync<BusinessException>(() => service.RequestAsync(other, Guid.NewGuid(), h.WarehouseId, destination.Id, h.SkuId, 2, "Not my warehouse"));
        var t = await service.RequestAsync(user, Guid.NewGuid(), h.WarehouseId, destination.Id, h.SkuId, 2, "Replenish");
        Assert.False(await service.Visible(other).AnyAsync(x => x.Id == t.Id));
        await Assert.ThrowsAsync<BusinessException>(() => service.DispatchAsync(user, t.Id, "X", "Not staff"));
        await Assert.ThrowsAsync<BusinessException>(() => service.ReceiveAsync(user, t.Id, "Not dispatched"));
        await service.DispatchAsync(staff, t.Id, "X", "Approved");
        await Assert.ThrowsAsync<BusinessException>(() => service.ReceiveAsync(other, t.Id, "Not recipient"));
        await Assert.ThrowsAsync<BusinessException>(() => service.CancelAsync(user, t.Id, "Already dispatched"));
        Assert.Equal(TransferStatus.Dispatched, t.Status);
    }
    [SqlFact] public async Task Insufficient_stock_rolls_back_and_requested_transfer_can_be_cancelled_once()
    {
        await using var h = new Harness(fixture); await h.InitializeAsync(2); var owner = await h.MemberAsync(); var user = Principal(owner); var staff = Principal(owner, "Warehouse");
        var destination = await h.Db.Warehouses.SingleAsync(x => x.OwnerMemberId == owner.Id); var service = Service(h.Db, h.Clock, user);
        var t = await service.RequestAsync(user, Guid.NewGuid(), h.WarehouseId, destination.Id, h.SkuId, 3, "Need stock");
        await Assert.ThrowsAsync<BusinessException>(() => service.DispatchAsync(staff, t.Id, "X", "Shortage"));
        Assert.Equal(2, await h.Db.InventoryBatches.Where(x => x.SkuId == h.SkuId).SumAsync(x => x.QtyAvailable)); Assert.Empty(t.Allocations);
        await service.CancelAsync(user, t.Id, "Cancel request"); await service.CancelAsync(user, t.Id, "Cancel request");
        Assert.Equal(1, await h.Db.AuditLogs.CountAsync(x => x.Action == "Transfer.Cancel" && x.Subject == t.Id.ToString()));
        await Assert.ThrowsAsync<BusinessException>(() => service.DispatchAsync(staff, t.Id, "X", "Cannot dispatch cancelled"));
    }
    [SqlFact] public async Task Conflicting_destination_lot_cannot_partially_receive_transfer()
    {
        await using var h = new Harness(fixture); await h.InitializeAsync(5); var owner = await h.MemberAsync(); var user = Principal(owner); var staff = Principal(owner, "Warehouse");
        var destination = await h.Db.Warehouses.SingleAsync(x => x.OwnerMemberId == owner.Id); var source = await h.Db.InventoryBatches.SingleAsync(x => x.WarehouseId == h.WarehouseId);
        await h.Inventory.ReceiveAsync(h.SkuId, destination.Id, source.LotNo, source.MfgDate, source.ExpDate, 1, 199, "Conflicting cost");
        var service = Service(h.Db, h.Clock, user); var t = await service.RequestAsync(user, Guid.NewGuid(), h.WarehouseId, destination.Id, h.SkuId, 2, "Replenish");
        await service.DispatchAsync(staff, t.Id, "X", "Approved");
        await Assert.ThrowsAsync<BusinessException>(() => service.ReceiveAsync(user, t.Id, "Conflicting lot"));
        await using var verify = fixture.Db(); Assert.Equal(TransferStatus.Dispatched, (await verify.StockTransfers.FindAsync(t.Id))!.Status);
        Assert.Equal(1, await verify.InventoryBatches.Where(x => x.WarehouseId == destination.Id).SumAsync(x => x.QtyAvailable));
        Assert.False(await verify.InventoryTransactions.AnyAsync(x => x.Kind == InventoryKind.TransferIn && x.Reason.StartsWith($"Transfer #{t.Id};")));
    }
    [SqlFact] public async Task Concurrent_transfers_cannot_overdraw_the_same_source_stock()
    {
        await using var h = new Harness(fixture); await h.InitializeAsync(10); var owner = await h.MemberAsync(); var user = Principal(owner); var staff = Principal(owner, "Warehouse");
        var destination = await h.Db.Warehouses.SingleAsync(x => x.OwnerMemberId == owner.Id); var service = Service(h.Db, h.Clock, user);
        var first = await service.RequestAsync(user, Guid.NewGuid(), h.WarehouseId, destination.Id, h.SkuId, 6, "First");
        var second = await service.RequestAsync(user, Guid.NewGuid(), h.WarehouseId, destination.Id, h.SkuId, 6, "Second");
        async Task<Exception?> Attempt(long id) { await using var db = fixture.Db(); try { await Service(db, h.Clock, staff).DispatchAsync(staff, id, "CONCURRENT", "Approved"); return null; } catch (Exception ex) { return ex; } }
        var outcomes = await Task.WhenAll(Attempt(first.Id), Attempt(second.Id)); Assert.Single(outcomes.Where(x => x == null));
        await using var verify = fixture.Db(); Assert.Equal(4, await verify.InventoryBatches.Where(x => x.WarehouseId == h.WarehouseId).SumAsync(x => x.QtyAvailable));
        var ids = new[] { first.Id, second.Id }; Assert.Equal(1, await verify.StockTransfers.CountAsync(x => ids.Contains(x.Id) && x.Status == TransferStatus.Dispatched));
        var waiting = await verify.StockTransfers.SingleAsync(x => ids.Contains(x.Id) && x.Status == TransferStatus.Requested);
        await Assert.ThrowsAsync<BusinessException>(() => Service(verify, h.Clock, staff).DispatchAsync(staff, waiting.Id, "RETRY", "Still insufficient"));
    }
    [SqlFact] public async Task Long_notes_fit_inventory_history_and_excess_length_is_rejected_before_writes()
    {
        await using var h = new Harness(fixture); await h.InitializeAsync(1); var owner = await h.MemberAsync(); var user = Principal(owner); var staff = Principal(owner, "Warehouse");
        var destination = await h.Db.Warehouses.SingleAsync(x => x.OwnerMemberId == owner.Id); var service = Service(h.Db, h.Clock, user);
        await Assert.ThrowsAsync<BusinessException>(() => service.RequestAsync(user, Guid.NewGuid(), h.WarehouseId, destination.Id, h.SkuId, 1, new string('ก', 901)));
        var note = new string('ก', 900); var t = await service.RequestAsync(user, Guid.NewGuid(), h.WarehouseId, destination.Id, h.SkuId, 1, note);
        await service.DispatchAsync(staff, t.Id, "LONG-NOTE", note); await service.ReceiveAsync(user, t.Id, note);
        Assert.Equal(TransferStatus.Received, t.Status);
        Assert.Equal(2, await h.Db.InventoryTransactions.CountAsync(x => x.Reason.StartsWith($"Transfer #{t.Id};")));
    }
}
