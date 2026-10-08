using Cardui.Api.Domain.Savings;
using Xunit;

namespace Cardui.Tests.Domain;

public class SavingsAmountTests
{
    [Fact]
    public void InUse_UsesTheTypedAmountWhenNothingIsFollowed()
    {
        Assert.Equal(25m, SavingsAmount.InUse(false, false, 25m, 80m));
        Assert.Equal(0m, SavingsAmount.InUse(false, false, -5m, 80m));
    }

    [Fact]
    public void InUse_UsesTheBalanceWhileFollowingWithoutAnOverride()
    {
        Assert.Equal(80m, SavingsAmount.InUse(true, false, 10m, 80m));
        Assert.Equal(0m, SavingsAmount.InUse(true, false, 10m, -5m));
    }

    [Fact]
    public void InUse_KeepsTheTypedAmountWhenItOverridesTheBalance()
    {
        Assert.Equal(10m, SavingsAmount.InUse(true, true, 10m, 80m));
        Assert.Equal(0m, SavingsAmount.InUse(true, true, -1m, 80m));
    }
}
