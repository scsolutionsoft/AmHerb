using System.Data;
using AmHerb.Web.Data;
using AmHerb.Web.Domain;
using Microsoft.EntityFrameworkCore;

namespace AmHerb.Web.Services;

public record WalletBalance(decimal Pending, decimal Available, decimal Reserved);
public static class RewardMath
{
    public static decimal[] Calculate(decimal basis, TokenPolicy p, bool verified, bool selfPurchase, bool channelAllowed, bool stockLoading)
    {
        if (!verified || !channelAllowed || stockLoading) return [0, 0, 0, 0];
        if (selfPurchase) return [p.LoyaltyEnabled ? Money.Round(basis * p.LoyaltyPercent / 100) : 0, 0, 0, 0];
        return [Money.Round(basis * p.SellerPercent / 100), p.MaxDepth >= 1 ? Money.Round(basis * p.Level1Percent / 100) : 0,
            p.MaxDepth >= 2 ? Money.Round(basis * p.Level2Percent / 100) : 0, p.MaxDepth >= 3 ? Money.Round(basis * p.Level3Percent / 100) : 0];
    }
    public static decimal Reversal(decimal original, int originalQuantity, int cumulativeReturned, decimal alreadyReversed)
    {
        if (originalQuantity <= 0 || cumulativeReturned < 0 || cumulativeReturned > originalQuantity) throw new BusinessException("จำนวนคืนไม่ถูกต้อง");
        return Money.Round(original * cumulativeReturned / originalQuantity) - alreadyReversed;
    }
}
public class RewardService(AmHerbDbContext db, PricingService pricing, TimeProvider clock, INotificationService notifications)
{
    public async Task<WalletBalance> BalanceAsync(long memberId)
    {
        var totals = await db.TokenLedger.Where(x => x.MemberId == memberId).GroupBy(x => x.MemberId)
            .Select(g => new { Pending = g.Sum(x => x.PendingDelta), Available = g.Sum(x => x.AvailableDelta), Reserved = g.Sum(x => x.ReservedDelta) }).SingleOrDefaultAsync();
        return totals == null ? new(0, 0, 0) : new(totals.Pending, totals.Available, totals.Reserved);
    }
    public async Task<decimal> QuoteAsync(long memberId, decimal eligibleMerchandise, SalesChannel channel)
    {
        var policy = await pricing.PolicyAsync();
        var channelPolicy = await db.ChannelPolicies.SingleAsync(x => x.Channel == channel);
        if (!channelPolicy.AllowRedemption) return 0;
        var balance = await BalanceAsync(memberId);
        return Math.Max(0, Math.Min(balance.Available, decimal.Floor(eligibleMerchandise * policy.MaxRedemptionPercent / 100 / policy.RedemptionReferenceThb * 100) / 100));
    }
    public async Task CreateForOrderAsync(Order order)
    {
        if (order.RewardsCreated) return;
        if(await db.CreditInvoices.AnyAsync(x=>x.OrderId==order.Id&&x.Amount>x.Paid+x.Adjusted))throw new BusinessException("รายการซื้อเชื่อยังชำระหนี้ไม่ครบ");
        if (!order.VerifiedRetailSale || order.Status != OrderStatus.Completed) throw new BusinessException("ต้องยืนยันยอดขายปลีกและส่งมอบสำเร็จก่อน");
        order.RewardsCreated = true;
        if (order.SellerMemberId == null || order.StockLoading) return;
        var seller = await db.Members.Include(x => x.User).SingleAsync(x => x.Id == order.SellerMemberId);
        if (seller.Status != MemberStatus.Active) return;
        var policy = await db.TokenPolicies.SingleAsync(x => x.Id == order.TokenPolicyVersion);
        var channel = await db.ChannelPolicies.SingleAsync(x => x.Channel == order.Channel);
        var self = order.BuyerMemberId == seller.Id ||
            (!string.IsNullOrWhiteSpace(order.Email) && string.Equals(order.Email.Trim(), seller.User.Email, StringComparison.OrdinalIgnoreCase)) ||
            (!string.IsNullOrWhiteSpace(order.Phone) && order.Phone.Trim() == seller.Phone && !string.IsNullOrWhiteSpace(seller.Phone));
        var ancestors = await db.MemberClosures.Where(x => x.DescendantMemberId == seller.Id && x.Depth >= 1 && x.Depth <= 3 && x.Ancestor.Status == MemberStatus.Active).ToListAsync();
        var now = clock.GetUtcNow().UtcDateTime;
        foreach (var item in order.Items.Where(x => !x.IsGift))
        {
            var amounts = RewardMath.Calculate(item.BaseToken * (item.Quantity - item.ReturnedQuantity) * item.TokenMultiplier, policy, true, self, channel.AllowNetworkReward, order.StockLoading);
            for (var level = 0; level <= 3; level++)
            {
                var recipient = level == 0 ? seller.Id : ancestors.FirstOrDefault(x => x.Depth == level)?.AncestorMemberId;
                if (recipient == null || amounts[level] <= 0) continue;
                var distribution = new TokenDistribution { MemberId = recipient.Value, OrderItemId = item.Id, SourceOrderId = order.Id, Level = level, Amount = amounts[level], Status = TokenStatus.Pending,
                    QuantityBasis = item.Quantity - item.ReturnedQuantity, ReturnedAtIssue = item.ReturnedQuantity, RewardPolicyVersion = policy.Id, TokenRateVersion = item.TokenRateVersion, ReleaseAt = now.AddDays(policy.PendingDays), ExpireAt = now.AddDays(policy.PendingDays).AddMonths(policy.ExpiryMonths) };
                db.TokenDistributions.Add(distribution);
                db.TokenLedger.Add(new TokenLedger { MemberId = recipient.Value, SourceOrderId = order.Id, OrderItemId = item.Id, Distribution = distribution,
                    Kind = self ? LedgerKind.LOYALTY : level switch { 0 => LedgerKind.SALE_REWARD, 1 => LedgerKind.UPLINE_L1, 2 => LedgerKind.UPLINE_L2, _ => LedgerKind.UPLINE_L3 },
                    Status = TokenStatus.Pending, PendingDelta = amounts[level], RewardPolicyVersion = policy.Id, TokenRateVersion = item.TokenRateVersion, EventKey = $"reward:{item.Id}:{level}", PostedAt = now });
                await notifications.AddAsync(recipient, $"pending:{item.Id}:{level}", $"Token รอปลดล็อก {amounts[level]:N2} จาก {order.Number}");
            }
        }
    }
    // Caller owns a serializable transaction for checkout, payment, cancellation and refund.
    public async Task ReserveAsync(Order order, decimal eligible)
    {
        if (order.TokenRedemption <= 0) return;
        if (order.BuyerMemberId == null) throw new BusinessException("เข้าสู่ระบบก่อนใช้ Token");
        var max = await QuoteAsync(order.BuyerMemberId.Value, eligible, order.Channel);
        if (order.TokenRedemption > max) throw new BusinessException("Token ไม่เพียงพอหรือเกินวงเงินสินค้าที่ร่วมรายการ");
        db.TokenLedger.Add(new TokenLedger { MemberId = order.BuyerMemberId.Value, SourceOrderId = order.Id, Kind = LedgerKind.RESERVATION, Status = TokenStatus.Reserved,
            AvailableDelta = -order.TokenRedemption, ReservedDelta = order.TokenRedemption, RewardPolicyVersion = order.TokenPolicyVersion, EventKey = $"reserve:{order.Id}" });
        var remaining = order.TokenRedemption;
        var lots = await db.TokenDistributions.Where(x => x.MemberId == order.BuyerMemberId && x.Status == TokenStatus.Available).OrderBy(x => x.ExpireAt).ThenBy(x => x.Id).ToListAsync();
        foreach (var lot in lots)
        {
            var consumed = await db.TokenConsumptions.Where(x => x.DistributionId == lot.Id).SumAsync(x => (decimal?)x.Amount) ?? 0;
            var take = Math.Min(remaining, Math.Max(0, lot.Amount - lot.ReversedAmount - lot.ExpiredAmount - consumed));
            if (take <= 0) continue;
            db.TokenConsumptions.Add(new TokenConsumption { DistributionId = lot.Id, OrderId = order.Id, Amount = take });
            remaining -= take;
            if (remaining <= 0) break;
        }
    }
    public async Task ConsumeAsync(Order order)
    {
        if (order.TokenRedemption <= 0 || await db.TokenLedger.AnyAsync(x => x.EventKey == $"redeem:{order.Id}")) return;
        db.TokenLedger.Add(new TokenLedger { MemberId = order.BuyerMemberId!.Value, SourceOrderId = order.Id, Kind = LedgerKind.REDEMPTION, Status = TokenStatus.Redeemed,
            ReservedDelta = -order.TokenRedemption, RewardPolicyVersion = order.TokenPolicyVersion, EventKey = $"redeem:{order.Id}" });
    }
    public async Task CancelReservationAsync(Order order)
    {
        if (order.TokenRedemption <= 0 || await db.TokenLedger.AnyAsync(x => x.EventKey == $"unreserve:{order.Id}")) return;
        db.TokenLedger.Add(new TokenLedger { MemberId = order.BuyerMemberId!.Value, SourceOrderId = order.Id, Kind = LedgerKind.RESERVATION_RELEASE, Status = TokenStatus.Available,
            AvailableDelta = order.TokenRedemption, ReservedDelta = -order.TokenRedemption, RewardPolicyVersion = order.TokenPolicyVersion, EventKey = $"unreserve:{order.Id}" });
        db.TokenConsumptions.RemoveRange(await db.TokenConsumptions.Where(x => x.OrderId == order.Id).ToListAsync());
    }
    public async Task ReverseItemAsync(OrderItem item, long returnId)
    {
        var distributions = await db.TokenDistributions.Where(x => x.OrderItemId == item.Id).ToListAsync();
        foreach (var d in distributions)
        {
            var amount = RewardMath.Reversal(d.Amount, d.QuantityBasis, item.ReturnedQuantity - d.ReturnedAtIssue, d.ReversedAmount);
            if (amount <= 0) continue;
            // Already expired reward must not create a second debit. Spent rewards do create debt.
            var debit = Math.Max(0, Math.Min(amount, d.Amount - d.ReversedAmount - d.ExpiredAmount));
            d.ReversedAmount += amount;
            db.TokenLedger.Add(new TokenLedger { MemberId = d.MemberId, SourceOrderId = d.SourceOrderId, OrderItemId = item.Id, DistributionId = d.Id,
                Kind = LedgerKind.RETURN_REVERSAL, Status = TokenStatus.Reversed, PendingDelta = d.Status == TokenStatus.Pending ? -debit : 0,
                AvailableDelta = d.Status == TokenStatus.Pending ? 0 : -debit, RewardPolicyVersion = d.RewardPolicyVersion, TokenRateVersion = d.TokenRateVersion, EventKey = $"reverse:{returnId}:{d.Id}" });
            if (d.ReversedAmount >= d.Amount) d.Status = TokenStatus.Reversed;
            await notifications.AddAsync(d.MemberId, $"reverse:{returnId}:{d.Id}", $"ปรับคืน Token {debit:N2} จากการคืนสินค้า");
        }
        await db.SaveChangesAsync();
        foreach (var id in distributions.Select(x => x.MemberId).Distinct())
            if ((await BalanceAsync(id)).Available < 0) (await db.Members.SingleAsync(x => x.Id == id)).ReviewRequired = true;
    }
    public async Task RestoreRedemptionAsync(Order order, decimal tokens, long returnId)
    {
        if (tokens <= 0 || order.BuyerMemberId == null) return;
        db.TokenLedger.Add(new TokenLedger { MemberId = order.BuyerMemberId.Value, SourceOrderId = order.Id, Kind = LedgerKind.RETURN_REVERSAL, Status = TokenStatus.Available,
            AvailableDelta = tokens, RewardPolicyVersion = order.TokenPolicyVersion, EventKey = $"restore:{returnId}" });
        var remaining = tokens;
        foreach (var c in await db.TokenConsumptions.Where(x => x.OrderId == order.Id).OrderByDescending(x => x.Id).ToListAsync())
        { var restore = Math.Min(remaining, c.Amount); c.Amount -= restore; remaining -= restore; if (remaining <= 0) break; }
    }
    public async Task RunDueAsync()
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var now = clock.GetUtcNow().UtcDateTime;
        foreach (var d in await db.TokenDistributions.Where(x => x.Status == TokenStatus.Pending && x.ReleaseAt <= now).ToListAsync())
        {
            var amount = d.Amount - d.ReversedAmount;
            db.TokenLedger.Add(new TokenLedger { MemberId = d.MemberId, SourceOrderId = d.SourceOrderId, OrderItemId = d.OrderItemId, DistributionId = d.Id,
                Kind = LedgerKind.RELEASE, Status = TokenStatus.Available, PendingDelta = -amount, AvailableDelta = amount,
                RewardPolicyVersion = d.RewardPolicyVersion, TokenRateVersion = d.TokenRateVersion, EventKey = $"release:{d.Id}", PostedAt = now });
            d.Status = TokenStatus.Available;
            await notifications.AddAsync(d.MemberId, $"release:{d.Id}", $"Token พร้อมใช้ {amount:N2}");
        }
        await db.SaveChangesAsync();
        foreach (var d in await db.TokenDistributions.Where(x => x.Status == TokenStatus.Available && x.ExpireAt <= now).ToListAsync())
        {
            var consumed = await db.TokenConsumptions.Where(x => x.DistributionId == d.Id).SumAsync(x => (decimal?)x.Amount) ?? 0;
            var amount = Math.Max(0, d.Amount - d.ReversedAmount - d.ExpiredAmount - consumed);
            if (amount <= 0) continue;
            d.ExpiredAmount += amount;
            db.TokenLedger.Add(new TokenLedger { MemberId = d.MemberId, SourceOrderId = d.SourceOrderId, OrderItemId = d.OrderItemId, DistributionId = d.Id,
                Kind = LedgerKind.EXPIRY, Status = TokenStatus.Expired, AvailableDelta = -amount, RewardPolicyVersion = d.RewardPolicyVersion, TokenRateVersion = d.TokenRateVersion,
                EventKey = $"expire:{d.Id}:{d.ExpiredAmount.ToString(System.Globalization.CultureInfo.InvariantCulture)}", PostedAt = now });
        }
        await db.SaveChangesAsync(); await tx.CommitAsync();
    }
}
