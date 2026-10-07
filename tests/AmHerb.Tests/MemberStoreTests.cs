using AmHerb.Web.Controllers;
using AmHerb.Web.Domain;
using AmHerb.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AmHerb.Tests;
[Collection("SQL")]
public class MemberStoreTests(SqlFixture fixture)
{
    private static Actor ActorFor(string userId) => new(new HttpContextAccessor { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId) }, "test")) } });
    private static MemberStoreService Service(Harness h, Member member) => new(h.Db, h.Members, new AuditService(h.Db, ActorFor(member.UserId)));
    private static CheckoutInput Input(string? key = null) => new() { Key = key ?? Guid.NewGuid().ToString("N"), CustomerName = "Store buyer", Phone = "0801234567", Address = "Test address" };
    private static async Task<MemberStore> Store(Harness h, Member member, int stock = 4)
    {
        var service = Service(h, member); var store = await service.CreateAsync(member.UserId);
        await service.SaveAsync(member.UserId, "Test shop", "Description", "0801234567", true);
        await service.SetProductAsync(member.UserId, h.SkuId, true);
        if (stock > 0) await h.Inventory.ReceiveAsync(h.SkuId, store.WarehouseId, "SHOP", h.Clock.Now.AddDays(-1), h.Clock.Now.AddYears(1), stock, 120, "Shop stock");
        return store;
    }
    [SqlFact] public async Task Store_creation_is_idempotent_and_orders_use_only_its_warehouse()
    {
        await using var h = new Harness(fixture); await h.InitializeAsync(); var owner = await h.MemberAsync(); var other = await h.MemberAsync();
        var store = await Store(h, owner); var another = await Store(h, other);
        Assert.Equal(store.Id, (await Service(h, owner).CreateAsync(owner.UserId)).Id);
        var input = Input(); var order = await h.Commerce.CheckoutAsync(input, [new(h.SkuId, 2)], null, other.Id, storeId: store.Id);
        Assert.Equal(owner.Id, order.SellerMemberId); Assert.Equal(store.Id, order.StoreId);
        Assert.Equal(store.Id, (await h.Commerce.CheckoutAsync(input, [new(h.SkuId, 2)], null, null, storeId: store.Id)).StoreId);
        var allocations = await h.Db.StockAllocations.Include(x => x.InventoryBatch).Where(x => x.OrderItem.OrderId == order.Id).ToListAsync();
        Assert.All(allocations, x => Assert.Equal(store.WarehouseId, x.InventoryBatch.WarehouseId));
        Assert.Equal(2, await h.Db.InventoryBatches.Where(x => x.WarehouseId == store.WarehouseId && x.SkuId == h.SkuId).SumAsync(x => x.QtyAvailable));
        Assert.Equal(4, await h.Db.InventoryBatches.Where(x => x.WarehouseId == another.WarehouseId && x.SkuId == h.SkuId).SumAsync(x => x.QtyAvailable));
        await Assert.ThrowsAsync<BusinessException>(() => Service(h, other).RequireOrderAsync(other.UserId, order.Id));
        await h.Commerce.CancelAsync(order.Id, "cancel"); await h.Commerce.CancelAsync(order.Id, "replay");
        Assert.Equal(4, await h.Db.InventoryBatches.Where(x => x.WarehouseId == store.WarehouseId && x.SkuId == h.SkuId).SumAsync(x => x.QtyAvailable));
    }
    [SqlFact] public async Task Store_rejects_cross_store_replay_disabled_products_and_company_credit()
    {
        await using var h = new Harness(fixture); await h.InitializeAsync(); var a = await h.MemberAsync(); var b = await h.MemberAsync(); var first = await Store(h, a); var second = await Store(h, b);
        var input = Input(); await h.Commerce.CheckoutAsync(input, [new(h.SkuId, 1)], null, null, storeId: first.Id);
        await Assert.ThrowsAsync<BusinessException>(() => h.Commerce.CheckoutAsync(input, [new(h.SkuId, 1)], null, null, storeId: second.Id));
        var credit = Input(); credit.UseCredit = true;
        await Assert.ThrowsAsync<BusinessException>(() => h.Commerce.CheckoutAsync(credit, [new(h.SkuId, 1)], a.Id, null, storeId: first.Id));
        await Service(h, a).SetProductAsync(a.UserId, h.SkuId, false);
        await Assert.ThrowsAsync<BusinessException>(() => h.Commerce.CheckoutAsync(Input(), [new(h.SkuId, 1)], null, null, storeId: first.Id));
        await Service(h, b).SaveAsync(b.UserId, "Paused", "", "0800000000", false);
        await Assert.ThrowsAsync<BusinessException>(() => h.Commerce.CheckoutAsync(Input(), [new(h.SkuId, 1)], null, null, storeId: second.Id));
    }
    [SqlFact] public async Task Store_expenses_are_scoped_immutable_and_reversed_once()
    {
        await using var h = new Harness(fixture); await h.InitializeAsync(); var a = await h.MemberAsync(); var b = await h.MemberAsync(); var store = await Store(h, a); await Store(h, b);
        var svc = Service(h, a); var key = Guid.NewGuid(); var day = DateTime.UtcNow.AddHours(7).Date;
        await svc.AddExpenseAsync(a.UserId, key, day, 50, "Packaging", "R1"); await svc.AddExpenseAsync(a.UserId, key, day, 50, "Packaging", "R1");
        var expense = await h.Db.StoreExpenses.SingleAsync(x => x.StoreId == store.Id);
        await Assert.ThrowsAsync<BusinessException>(() => Service(h, b).ReverseExpenseAsync(b.UserId, expense.Id, "other shop"));
        await svc.ReverseExpenseAsync(a.UserId, expense.Id, "Mistake"); await svc.ReverseExpenseAsync(a.UserId, expense.Id, "Replay");
        Assert.Equal(0, await h.Db.StoreExpenses.Where(x => x.StoreId == store.Id).SumAsync(x => x.Amount));
        expense.Amount = 3; await Assert.ThrowsAsync<InvalidOperationException>(() => h.Db.SaveChangesAsync());
    }
    [SqlFact] public async Task Network_page_exposes_only_the_signed_in_members_rewards()
    {
        await using var h = new Harness(fixture); await h.InitializeAsync(); var parent = await h.MemberAsync(); var seller = await h.MemberAsync(parent); var outsider = await h.MemberAsync();
        var order = await h.SellAsync(seller); await h.Commerce.VerifyRetailAsync(order.Id, "Retail");
        var page = new NetworkTokensController(h.Db, h.Members, h.Rewards, ActorFor(parent.UserId));
        var model = Assert.IsType<NetworkTokensPage>(Assert.IsType<ViewResult>(await page.Index(null)).Model);
        Assert.Single(model.Rewards); Assert.Equal(1, model.Rewards[0].Level); Assert.Equal(18, model.Wallet.Pending);
        Assert.DoesNotContain(model.Relations, x => x.Code == outsider.Code);
        var otherPage = new NetworkTokensController(h.Db, h.Members, h.Rewards, ActorFor(outsider.UserId));
        var otherModel = Assert.IsType<NetworkTokensPage>(Assert.IsType<ViewResult>(await otherPage.Index(null)).Model);
        Assert.Empty(otherModel.Rewards); Assert.Equal(0, otherModel.Wallet.Pending);
    }
    [SqlFact] public async Task Cart_cookie_for_one_store_cannot_load_another_stores_cart()
    {
        await using var h = new Harness(fixture); await h.InitializeAsync(); var a = await h.MemberAsync(); var b = await h.MemberAsync(); var first = await Store(h, a); var second = await Store(h, b);
        var cart = new Cart { StoreId = first.Id }; h.Db.Carts.Add(cart); await h.Db.SaveChangesAsync();
        var context = new DefaultHttpContext(); context.Request.Headers.Cookie = $"am.cart.store.{second.Id}={cart.PublicId}; am.cart={cart.PublicId}";
        var carts = new CartService(h.Db, new HttpContextAccessor { HttpContext = context }, h.Prices);
        var other = await carts.GetAsync(second.Id); var company = await carts.GetAsync();
        Assert.NotEqual(cart.Id, other.Id); Assert.Equal(second.Id, other.StoreId); Assert.Null(company.StoreId); Assert.NotEqual(cart.Id, company.Id);
    }
    [SqlFact] public async Task Online_and_POS_cannot_sell_the_same_last_item()
    {
        await using var h = new Harness(fixture); await h.InitializeAsync(); var owner = await h.MemberAsync(); var store = await Store(h, owner, 1);
        (await h.Db.PosRegisters.SingleAsync(x => x.UserId == owner.UserId)).WarehouseId = store.WarehouseId; await h.Db.SaveChangesAsync();
        var session = await h.OpenAsync(owner);
        async Task<bool> Attempt(bool online)
        {
            await using var attempt = new Harness(fixture);
            try
            {
                if (online) await attempt.Commerce.CheckoutAsync(Input(), [new(h.SkuId, 1)], null, null, storeId: store.Id);
                else await attempt.Commerce.CheckoutAsync(Input(), [new(h.SkuId, 1)], null, owner.Id, SalesChannel.POS, owner.UserId, session.Id, 1000);
                return true;
            }
            catch (Exception e) when (e is BusinessException or DbUpdateException || DatabaseConflict.IsDeadlock(e)) { return false; }
        }
        var result = await Task.WhenAll(Attempt(true), Attempt(false)); Assert.Single(result.Where(x => x));
        h.Db.ChangeTracker.Clear(); var batch = await h.Db.InventoryBatches.SingleAsync(x => x.WarehouseId == store.WarehouseId && x.SkuId == h.SkuId);
        Assert.Equal(0, batch.QtyAvailable); Assert.InRange(batch.QtyReserved, 0, 1);
    }
}
