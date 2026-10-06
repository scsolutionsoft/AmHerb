using AmHerb.Web.Domain;
using AmHerb.Web.Services;

namespace AmHerb.Tests;
public class RewardTests
{
    [Theory]
    [InlineData(90, 90, 18, 6.3, 2.7, 117)]
    [InlineData(60, 60, 12, 4.2, 1.8, 78)]
    [InlineData(80, 80, 16, 5.6, 2.4, 104)]
    public void Seed_examples_match_exactly(decimal basis, decimal seller, decimal l1, decimal l2, decimal l3, decimal total)
    {
        var amounts = RewardMath.Calculate(basis, new TokenPolicy(), true, false, true, false);
        Assert.Equal(new[] { seller, l1, l2, l3 }, amounts); Assert.Equal(total, amounts.Sum());
    }
    [Theory]
    [InlineData(false, false, true, false)]
    [InlineData(true, true, true, false)]
    [InlineData(true, false, false, false)]
    [InlineData(true, false, true, true)]
    public void Ineligible_sales_never_issue_network_rewards(bool verified, bool self, bool allowed, bool loading)
        => Assert.All(RewardMath.Calculate(90, new TokenPolicy(), verified, self, allowed, loading), x => Assert.Equal(0, x));
    [Fact] public void Loyalty_does_not_pay_ancestors()
        => Assert.Equal(new decimal[] { 9, 0, 0, 0 }, RewardMath.Calculate(90, new TokenPolicy { LoyaltyEnabled = true, LoyaltyPercent = 10 }, true, true, true, false));
    [Fact] public void Partial_reversals_preserve_total_with_rounding()
    {
        decimal reversed = 0;
        for (var quantity = 1; quantity <= 3; quantity++) reversed += RewardMath.Reversal(10, 3, quantity, reversed);
        Assert.Equal(10, reversed);
    }
    [Fact] public void Reversal_rejects_over_return() => Assert.Throws<BusinessException>(() => RewardMath.Reversal(10, 2, 3, 0));
    [Fact] public void Sponsor_cycle_and_self_are_rejected()
    { Assert.Throws<BusinessException>(() => MemberService.ValidateMove(1, 1, [])); Assert.Throws<BusinessException>(() => MemberService.ValidateMove(1, 3, [2, 3, 4])); MemberService.ValidateMove(1, 5, [2, 3, 4]); }
    [Fact] public void Policy_depth_controls_network_without_changing_seller()
        => Assert.Equal(new decimal[] { 90, 18, 0, 0 }, RewardMath.Calculate(90, new TokenPolicy { MaxDepth = 1 }, true, false, true, false));
}
