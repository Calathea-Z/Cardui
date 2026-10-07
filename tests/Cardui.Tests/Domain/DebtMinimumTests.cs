using Cardui.Api.Domain.Recovery;
using Cardui.Api.Models;
using Xunit;

namespace Cardui.Tests.Domain;

public class DebtMinimumTests
{
    [Fact]
    public void Resolve_UsesAStoredMinimumIncludingZero()
    {
        Assert.Equal(25m, DebtMinimum.Resolve(DebtKind.Revolving, 400m, 19.99m, 25m, null));
        Assert.Equal(0m, DebtMinimum.Resolve(DebtKind.Revolving, 400m, 19.99m, 0m, null));
    }

    [Fact]
    public void Resolve_CalculatesAZeroInterestInstallmentPayment()
    {
        var payment = DebtMinimum.Resolve(DebtKind.Installment, 1000m, 0m, null, 10);

        Assert.Equal(100m, payment);
    }

    [Fact]
    public void Resolve_CalculatesTwelvePercentOverTwelveMonths()
    {
        var payment = DebtMinimum.Resolve(DebtKind.Installment, 1000m, 12m, null, 12);

        Assert.Equal(88.85m, payment);
    }

    [Fact]
    public void Resolve_IncludesInterestInAOneMonthPayoff()
    {
        var payment = DebtMinimum.Resolve(DebtKind.Installment, 1000m, 12m, null, 1);

        Assert.Equal(1010m, payment);
    }

    [Fact]
    public void Resolve_LeavesARevolvingBlankMinimumUnknown()
    {
        Assert.Null(DebtMinimum.Resolve(DebtKind.Revolving, 400m, 19.99m, null, null));
        Assert.Null(DebtMinimum.Resolve(DebtKind.Installment, 400m, 12m, null, null));
    }

    [Fact]
    public void Resolve_LeavesAnOverflowingRateUnknown()
    {
        var payment = DebtMinimum.Resolve(DebtKind.Installment, 1000m, 999.999m, null, 600);

        Assert.Null(payment);
    }
}
