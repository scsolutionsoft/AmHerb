using AmHerb.Web.Domain;
using AmHerb.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace AmHerb.Tests;
[Collection("SQL")]
public class CommerceIntegrationTests(SqlFixture fixture)
{
    [SqlFact] public async Task Completed_retail_sale_snapshots_three_ancestors_and_releases_once()
    {
        await using var h = new Harness(fixture); await h.InitializeAsync();
        var a = await h.MemberAsync(); var b = await h.MemberAsync(a); var c = await h.MemberAsync(b); var d = await h.MemberAsync(c);
        var order = await h.SellAsync(d); Assert.Equal(OrderStatus.Delivered, order.Status); Assert.Empty(await h.Db.TokenDistributions.Where(x => x.SourceOrderId == order.Id).ToListAsync());
        await h.Commerce.VerifyRetailAsync(order.Id, "Customer received retail goods");
        var distributions = await h.Db.TokenDistributions.Where(x => x.SourceOrderId == order.Id).OrderBy(x => x.Level).ToListAsync();
        Assert.Equal(new decimal[] { 90, 18, 6.3m, 2.7m }, distributions.Select(x => x.Amount)); Assert.Equal(new[] { d.Id, c.Id, b.Id, a.Id }, distributions.Select(x => x.MemberId));
        await h.Members.MoveAsync(d.Id, a.Id, "Move after sale"); Assert.Equal(c.Id, distributions[1].MemberId);
        h.Clock.Now = h.Clock.Now.AddDays(15); await h.Rewards.RunDueAsync(); await h.Rewards.RunDueAsync();
        Assert.Equal(90, (await h.Rewards.BalanceAsync(d.Id)).Available); Assert.Equal(0, (await h.Rewards.BalanceAsync(d.Id)).Pending);
        Assert.Equal(4, await h.Db.TokenLedger.CountAsync(x => x.SourceOrderId == order.Id && x.Kind == LedgerKind.RELEASE));
    }
    [SqlFact] public async Task Self_purchase_and_stock_loading_do_not_create_network_rewards()
    {
        await using var h = new Harness(fixture); await h.InitializeAsync(); var m = await h.MemberAsync();
        var self = await h.SellAsync(m, buyer: m); await h.Commerce.VerifyRetailAsync(self.Id, "Self purchase");
        Assert.Empty(await h.Db.TokenDistributions.Where(x => x.SourceOrderId == self.Id).ToListAsync());
        var loading = await h.SellAsync(m, loading: true); await Assert.ThrowsAsync<BusinessException>(() => h.Commerce.VerifyRetailAsync(loading.Id, "Inventory purchase"));
    }
    [SqlFact] public async Task Partial_and_full_return_reverse_every_original_recipient_exactly()
    {
        await using var h = new Harness(fixture); await h.InitializeAsync(); var a = await h.MemberAsync(); var seller = await h.MemberAsync(a);
        var o = await h.SellAsync(seller, 3); await h.Commerce.VerifyRetailAsync(o.Id, "Retail proof");
        var item = o.Items.Single(); await h.Commerce.RequestReturnAsync(item.Id, 1, "Partial return");
        var first = await h.Db.ReturnRequests.SingleAsync(x => x.OrderItemId == item.Id);
        await h.Commerce.ApproveReturnAsync(first.Id, true, "REFUND-1", "Approved");
        Assert.Equal(180, (await h.Rewards.BalanceAsync(seller.Id)).Pending); Assert.Equal(36, (await h.Rewards.BalanceAsync(a.Id)).Pending);
        await h.Commerce.RequestReturnAsync(item.Id, 2, "Remaining return"); var second = await h.Db.ReturnRequests.SingleAsync(x => x.OrderItemId == item.Id && x.Status == "Requested");
        await h.Commerce.ApproveReturnAsync(second.Id, true, "REFUND-2", "Approved"); await h.Commerce.ApproveReturnAsync(second.Id, true, "REFUND-2", "Replay");
        Assert.Equal(0, (await h.Rewards.BalanceAsync(seller.Id)).Pending); Assert.Equal(0, (await h.Rewards.BalanceAsync(a.Id)).Pending);
        Assert.Equal(o.CashPayable, first.CashRefund + second.CashRefund); Assert.Equal(100, await h.Db.InventoryBatches.Where(x => x.SkuId == h.SkuId).SumAsync(x => x.QtyAvailable));
    }
    [SqlFact] public async Task Reserved_tokens_release_on_cancellation_and_consumed_tokens_can_create_refund_debt()
    {
        await using var h = new Harness(fixture); await h.InitializeAsync(); var seller = await h.MemberAsync();
        var source = await h.SellAsync(seller); await h.Commerce.VerifyRetailAsync(source.Id, "Retail proof"); h.Clock.Now = h.Clock.Now.AddDays(15); await h.Rewards.RunDueAsync();
        await h.Inventory.ReceiveAsync(h.SkuId, 1, "STORE", DateTime.UtcNow.AddDays(-30), DateTime.UtcNow.AddYears(2), 20, 100, "Store stock");
        CheckoutInput Input() => new() { Key = Guid.NewGuid().ToString("N"), CustomerName = "Member", Phone = "0901111111", Address = "Test address", Tokens = 90 };
        var cancelled = await h.Commerce.CheckoutAsync(Input(), [new(h.SkuId, 1)], seller.Id, null);
        Assert.Equal(0, (await h.Rewards.BalanceAsync(seller.Id)).Available); Assert.Equal(90, (await h.Rewards.BalanceAsync(seller.Id)).Reserved);
        await h.Commerce.CancelAsync(cancelled.Id, "Test cancel"); Assert.Equal(90, (await h.Rewards.BalanceAsync(seller.Id)).Available);
        var redeemed = await h.Commerce.CheckoutAsync(Input(), [new(h.SkuId, 1)], seller.Id, null);
        await h.Commerce.ConfirmPaymentAsync(redeemed.Id, "TEST-" + Guid.NewGuid(), redeemed.CashPayable, "Bank evidence");
        Assert.Equal(0, (await h.Rewards.BalanceAsync(seller.Id)).Reserved);
        await h.Commerce.RequestReturnAsync(source.Items.Single().Id, 1, "Returned after spending rewards");
        var request = await h.Db.ReturnRequests.SingleAsync(x => x.OrderItem.OrderId == source.Id);
        await h.Commerce.ApproveReturnAsync(request.Id, true, "REFUND-" + Guid.NewGuid(), "Approved");
        Assert.Equal(-90, (await h.Rewards.BalanceAsync(seller.Id)).Available); Assert.True(seller.ReviewRequired);
        Assert.Equal(0, await h.Rewards.QuoteAsync(seller.Id, 1000, SalesChannel.Store));
    }
    [SqlFact] public async Task FEFO_ignores_expired_lots_and_allocates_earliest_first()
    {
        await using var h = new Harness(fixture); await h.InitializeAsync(stock: 0); var seller = await h.MemberAsync();
        await h.Inventory.ReceiveAsync(h.SkuId, h.WarehouseId, "LATE", h.Clock.Now.AddDays(-30), h.Clock.Now.AddDays(90), 5, 100, "Receive late");
        await h.Inventory.ReceiveAsync(h.SkuId, h.WarehouseId, "EARLY", h.Clock.Now.AddDays(-30), h.Clock.Now.AddDays(10), 2, 100, "Receive early");
        var o = await h.SellAsync(seller, 3); var allocations = await h.Db.StockAllocations.Include(x => x.InventoryBatch).Where(x => x.OrderItem.OrderId == o.Id).OrderBy(x => x.Id).ToListAsync();
        Assert.Equal("EARLY", allocations[0].InventoryBatch.LotNo); Assert.Equal(2, allocations[0].Quantity); Assert.Equal(1, allocations[1].Quantity);
        Assert.Equal(0, await h.Db.InventoryBatches.Where(x => x.SkuId == h.SkuId).SumAsync(x => x.QtyReserved));
    }
    [SqlFact] public async Task POS_replay_does_not_duplicate_stock_payment_or_sale_and_close_reconciles()
    {
        await using var h = new Harness(fixture); await h.InitializeAsync(); var seller = await h.MemberAsync(); var session = await h.OpenAsync(seller, 500);
        var key = Guid.NewGuid().ToString("N"); var first = await h.SellAsync(seller, key: key); var second = await h.SellAsync(seller, key: key);
        Assert.Equal(first.Id, second.Id); Assert.Equal(99, await h.Db.InventoryBatches.Where(x => x.SkuId == h.SkuId).SumAsync(x => x.QtyAvailable));
        await h.Pos.CloseAsync(seller.UserId, session.Id, 1490, "Counted cash"); Assert.Equal(0, session.Difference); Assert.Equal(1490, session.ExpectedCash);
        await Assert.ThrowsAsync<BusinessException>(() => h.Commerce.CheckoutAsync(new CheckoutInput { Key = Guid.NewGuid().ToString("N"), CustomerName = "Customer", Phone = "0" }, [new(h.SkuId, 1)], null, seller.Id, SalesChannel.POS, seller.UserId, session.Id, 1000));
    }
    [SqlFact] public async Task POS_cannot_sell_or_close_using_another_users_session()
    {
        await using var h = new Harness(fixture); await h.InitializeAsync(); var one = await h.MemberAsync(); var two = await h.MemberAsync(); var session = await h.OpenAsync(one);
        await Assert.ThrowsAsync<BusinessException>(() => h.Pos.CloseAsync(two.UserId, session.Id, 0, "Unauthorized"));
        await Assert.ThrowsAsync<BusinessException>(() => h.Commerce.CheckoutAsync(new CheckoutInput { Key = Guid.NewGuid().ToString("N") }, [new(h.SkuId, 1)], null, two.Id, SalesChannel.POS, two.UserId, session.Id, 1000));
    }
    [SqlFact] public async Task Insufficient_stock_rolls_back_order_and_allocations()
    {
        await using var h = new Harness(fixture); await h.InitializeAsync(stock: 1); var seller = await h.MemberAsync(); var key = Guid.NewGuid().ToString("N");
        await Assert.ThrowsAsync<BusinessException>(() => h.SellAsync(seller, 2, key: key));
        await using var verification = fixture.Db(); Assert.False(await verification.Orders.AnyAsync(x => x.IdempotencyKey == key));
        Assert.Equal(1, await verification.InventoryBatches.Where(x => x.SkuId == h.SkuId).SumAsync(x => x.QtyAvailable));
    }
    [SqlFact] public async Task Effective_dates_and_channel_override_choose_correct_price_and_token_rate()
    {
        await using var h = new Harness(fixture); await h.InitializeAsync(); var tomorrow = h.Clock.Now.AddDays(1);
        h.Db.ProductPrices.Add(new ProductPrice { SkuId = h.SkuId, Amount = 800, Channel = SalesChannel.POS, EffectiveFrom = tomorrow });
        h.Db.TokenRates.Add(new TokenRate { SkuId = h.SkuId, BaseToken = 50, Channel = SalesChannel.POS, EffectiveFrom = tomorrow }); await h.Db.SaveChangesAsync();
        Assert.Equal(990, (await h.Prices.PriceAsync(h.SkuId, SalesChannel.POS)).Amount); h.Clock.Now = tomorrow.AddSeconds(1);
        Assert.Equal(800, (await h.Prices.PriceAsync(h.SkuId, SalesChannel.POS)).Amount); Assert.Equal(990, (await h.Prices.PriceAsync(h.SkuId, SalesChannel.Store)).Amount); Assert.Equal(50, (await h.Prices.RateAsync(h.SkuId, SalesChannel.POS)).BaseToken);
    }
    [SqlFact] public async Task Closure_move_is_atomic_and_cycle_rejected()
    {
        await using var h = new Harness(fixture); await h.InitializeAsync(); var a = await h.MemberAsync(); var b = await h.MemberAsync(a); var c = await h.MemberAsync(b); var d = await h.MemberAsync();
        await Assert.ThrowsAsync<BusinessException>(() => h.Members.MoveAsync(a.Id, c.Id, "Would cycle"));
        await h.Members.MoveAsync(b.Id, d.Id, "Move subtree");
        Assert.False(await h.Db.MemberClosures.AnyAsync(x => x.AncestorMemberId == a.Id && x.DescendantMemberId == c.Id));
        Assert.Equal(2, (await h.Db.MemberClosures.SingleAsync(x => x.AncestorMemberId == d.Id && x.DescendantMemberId == c.Id)).Depth);
    }
    [SqlFact] public async Task Marketplace_policy_blocks_network_and_seed_keeps_detox_wholesale_blank()
    {
        await using var h = new Harness(fixture); await h.InitializeAsync(); var seller = await h.MemberAsync();
        await h.Inventory.ReceiveAsync(h.SkuId, 1, "MARKET", h.Clock.Now.AddDays(-30), h.Clock.Now.AddYears(1), 10, 100, "Marketplace stock");
        var o = await h.Commerce.CheckoutAsync(new CheckoutInput { Key = Guid.NewGuid().ToString("N"), CustomerName = "Retail", Phone = "0900000000" }, [new(h.SkuId, 1)], null, seller.Id, SalesChannel.Shopee);
        await h.Commerce.ConfirmPaymentAsync(o.Id, "MARKET-" + Guid.NewGuid(), o.CashPayable, "Verified"); await h.Commerce.FulfillAsync(o.Id, "Test", "TEST", false, "Shipped"); await h.Commerce.FulfillAsync(o.Id, "Test", "TEST", true, "Delivered"); await h.Commerce.VerifyRetailAsync(o.Id, "Verified retail");
        Assert.False(await h.Db.TokenDistributions.AnyAsync(x => x.SourceOrderId == o.Id));
        Assert.False(await h.Db.ProductPrices.AnyAsync(x => x.SkuId == 4 && (x.Tier == "Wholesale" || x.Tier == "Pack6" || x.Tier == "Pack12")));
    }
    [SqlFact] public async Task Expiry_is_idempotent_and_never_expires_spent_tokens_again()
    {
        await using var h = new Harness(fixture); await h.InitializeAsync(); var seller = await h.MemberAsync(); var o = await h.SellAsync(seller); await h.Commerce.VerifyRetailAsync(o.Id, "Retail");
        h.Clock.Now = h.Clock.Now.AddDays(15); await h.Rewards.RunDueAsync();
        h.Clock.Now = h.Clock.Now.AddYears(2); await h.Rewards.RunDueAsync(); await h.Rewards.RunDueAsync();
        Assert.Equal(0, (await h.Rewards.BalanceAsync(seller.Id)).Available); Assert.Equal(1, await h.Db.TokenLedger.CountAsync(x => x.SourceOrderId == o.Id && x.Kind == LedgerKind.EXPIRY));
        await h.Commerce.RequestReturnAsync(o.Items.Single().Id, 1, "Return expired reward sale"); var r = await h.Db.ReturnRequests.SingleAsync(x => x.OrderItem.OrderId == o.Id);
        await h.Commerce.ApproveReturnAsync(r.Id, false, "REF", "Approved"); Assert.Equal(0, (await h.Rewards.BalanceAsync(seller.Id)).Available);
    }
    [SqlFact] public async Task Ledger_cannot_be_updated_through_EF_or_SQL()
    {
        await using var h = new Harness(fixture); await h.InitializeAsync(); var seller = await h.MemberAsync(); var o = await h.SellAsync(seller); await h.Commerce.VerifyRetailAsync(o.Id, "Retail");
        var entry = await h.Db.TokenLedger.SingleAsync(x => x.SourceOrderId == o.Id); entry.PendingDelta = 10000;
        await Assert.ThrowsAsync<InvalidOperationException>(() => h.Db.SaveChangesAsync()); h.Db.Entry(entry).State = EntityState.Unchanged;
        await Assert.ThrowsAsync<Microsoft.Data.SqlClient.SqlException>(() => h.Db.Database.ExecuteSqlInterpolatedAsync($"UPDATE TokenLedger SET PendingDelta = 10000 WHERE Id = {entry.Id}"));
    }
    [SqlFact] public async Task POS_redemption_requires_customer_consent_bound_to_register_and_is_single_use()
    {
        await using var h = new Harness(fixture); await h.InitializeAsync(); var customer = await h.MemberAsync(); var seller = await h.MemberAsync();
        var earned = await h.SellAsync(customer); await h.Commerce.VerifyRetailAsync(earned.Id, "Retail sale"); h.Clock.Now = h.Clock.Now.AddDays(15); await h.Rewards.RunDueAsync();
        var session = await h.OpenAsync(seller); var register = await h.Db.PosRegisters.SingleAsync(x => x.UserId == seller.UserId);
        var consent = new RedemptionConsentService(h.Db, h.Rewards, h.Clock); var code = await consent.IssueAsync(customer.Id, register.Id, 90);
        CheckoutInput Input(string approval) => new() { Key = Guid.NewGuid().ToString("N"), CustomerName = "Customer", Phone = "0911111111", Tokens = 90, RedemptionCode = approval };
        await Assert.ThrowsAsync<BusinessException>(() => h.Commerce.CheckoutAsync(Input("INVALID"), [new(h.SkuId, 1)], customer.Id, seller.Id, SalesChannel.POS, seller.UserId, session.Id, 1000));
        var paid = await h.Commerce.CheckoutAsync(Input(code), [new(h.SkuId, 1)], null, seller.Id, SalesChannel.POS, seller.UserId, session.Id, 1000);
        Assert.Equal(customer.Id, paid.BuyerMemberId); Assert.Equal(900, paid.CashPayable); Assert.Equal(0, (await h.Rewards.BalanceAsync(customer.Id)).Available);
        await Assert.ThrowsAsync<BusinessException>(() => h.Commerce.CheckoutAsync(Input(code), [new(h.SkuId, 1)], customer.Id, seller.Id, SalesChannel.POS, seller.UserId, session.Id, 1000));
        Assert.Equal(paid.Id, (await h.Db.RedemptionAuthorizations.SingleAsync(x => x.CodeHash == RedemptionConsentService.Hash(code))).UsedOrderId);
    }
    [SqlFact] public async Task Wholesale_pack_checkout_allocates_physical_units_and_excludes_network_reward()
    {
        await using var h = new Harness(fixture); await h.InitializeAsync(); var buyer = await h.MemberAsync(); var seller = await h.MemberAsync(); var session = await h.OpenAsync(seller);
        h.Db.ProductPrices.Add(new ProductPrice { SkuId = h.SkuId, Tier = "Pack6", PackQuantity = 6, Amount = 3000, EffectiveFrom = h.Clock.Now.AddDays(-1) }); await h.Db.SaveChangesAsync();
        var order = await h.Commerce.CheckoutAsync(new CheckoutInput { Key = Guid.NewGuid().ToString("N"), CustomerName = "Reseller", Phone = "0911111111" }, [new(h.SkuId, 2, "Pack6")], buyer.Id, seller.Id, SalesChannel.POS, seller.UserId, session.Id, 6000);
        Assert.Equal(6000, order.Merchandise); Assert.Equal(12, order.Items.Single().Quantity); Assert.True(order.StockLoading);
        Assert.Equal(88, await h.Db.InventoryBatches.Where(x => x.SkuId == h.SkuId).SumAsync(x => x.QtyAvailable));
        await Assert.ThrowsAsync<BusinessException>(() => h.Commerce.VerifyRetailAsync(order.Id, "No reward for stock"));
    }
    [SqlFact] public async Task Concurrent_cashiers_cannot_oversell_the_last_item()
    {
        await using var h = new Harness(fixture); await h.InitializeAsync(stock: 1); var first = await h.MemberAsync(); var second = await h.MemberAsync(); var s1 = await h.OpenAsync(first); var s2 = await h.OpenAsync(second);
        async Task<bool> Sell(Member m, PosSession s)
        {
            await using var attempt = new Harness(fixture);
            try { await attempt.Commerce.CheckoutAsync(new CheckoutInput { Key = Guid.NewGuid().ToString("N"), CustomerName = "Concurrent customer", Phone = "09" }, [new(h.SkuId, 1)], null, m.Id, SalesChannel.POS, m.UserId, s.Id, 1000); return true; }
            catch (Exception e) when (e is BusinessException || DatabaseConflict.IsDeadlock(e)) { return false; }
        }
        var results = await Task.WhenAll(Sell(first, s1), Sell(second, s2)); Assert.Equal(1, results.Count(x => x));
        await using var verification = fixture.Db(); Assert.Equal(0, await verification.InventoryBatches.Where(x => x.SkuId == h.SkuId).SumAsync(x => x.QtyAvailable));
    }
    [SqlFact] public async Task Free_gift_inventory_and_coupon_usage_roll_back_with_failed_checkout()
    {
        await using var h = new Harness(fixture); await h.InitializeAsync(stock: 1); var seller = await h.MemberAsync(); var session = await h.OpenAsync(seller);
        var promo = new Promotion { Code = Guid.NewGuid().ToString("N").ToUpperInvariant(), Kind = PromotionKind.FreeGift, GiftSkuId = h.SkuId, RequiredQuantity = 1, EffectiveFrom = h.Clock.Now.AddDays(-1), EffectiveTo = h.Clock.Now.AddDays(2), UsageLimit = 1, Reason = "Test promotion" };
        h.Db.Promotions.Add(promo); await h.Db.SaveChangesAsync();
        await Assert.ThrowsAsync<BusinessException>(() => h.Commerce.CheckoutAsync(new CheckoutInput { Key = Guid.NewGuid().ToString("N"), CustomerName = "Gift customer", Phone = "09", Coupon = promo.Code }, [new(h.SkuId, 1)], null, seller.Id, SalesChannel.POS, seller.UserId, session.Id, 1000));
        await using var verification = fixture.Db(); Assert.Equal(0, (await verification.Promotions.SingleAsync(x => x.Id == promo.Id)).UsedCount); Assert.Equal(1, await verification.InventoryBatches.Where(x => x.SkuId == h.SkuId).SumAsync(x => x.QtyAvailable));
    }
}
