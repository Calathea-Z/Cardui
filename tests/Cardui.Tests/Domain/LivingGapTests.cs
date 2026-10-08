using Cardui.Api.Domain.Living;
using Cardui.Api.Domain.Recovery;
using Cardui.Api.Models;
using Xunit;

namespace Cardui.Tests.Domain;

public class LivingGapTests
{
    [Fact]
    public void Measure_NamesAShortfallWithoutDroppingUnknownAmounts()
    {
        var gap = LivingGapCalculator.Measure(
            1000m,
            [
                new DatedBill(Guid.NewGuid(), "Rent", "USD", 800m, ObligationCadence.Monthly, new DateOnly(2026, 10, 1)),
                new DatedBill(Guid.NewGuid(), "Gift", "USD", 40m, ObligationCadence.Irregular, new DateOnly(2026, 12, 1))
            ],
            [
                new LivingDebtMinimum("Card", 500m, null, "USD"),
                new LivingDebtMinimum("Store", 1000m, 100m, "USD")
            ],
            [new LivingSpendingAmount("Monthly living spending", 400m, "USD"), new LivingSpendingAmount("Other living spending", 50m, "EUR")],
            "USD");

        Assert.Equal(1000m, gap.SharedMonthly);
        Assert.Equal(800m, gap.BillsMonthly);
        Assert.Equal(100m, gap.MinimumsMonthly);
        Assert.Equal(400m, gap.LivingSpendingMonthly);
        Assert.Equal(300m, gap.Shortfall);
        Assert.Contains("Gift has no monthly schedule.", gap.LeftOut);
        Assert.Contains("Card has no minimum yet.", gap.LeftOut);
        Assert.Contains("Other living spending is in EUR.", gap.LeftOut);
    }

    [Fact]
    public void Measure_IsCoveredWhenSharedPayMeetsTheMonthlyPicture()
    {
        var gap = LivingGapCalculator.Measure(
            2000m,
            [new DatedBill(Guid.NewGuid(), "Rent", "USD", 800m, ObligationCadence.Monthly, new DateOnly(2026, 10, 1))],
            [new LivingDebtMinimum("Card", 1000m, 200m, "USD")],
            [new LivingSpendingAmount("Monthly living spending", 150m, "USD")],
            "USD");

        Assert.Equal(0m, gap.Shortfall);
        Assert.Equal(1150m, gap.BillsMonthly + gap.MinimumsMonthly + gap.LivingSpendingMonthly);
    }

    [Fact]
    public void Measure_ExcludesAStoredMinimumWhenTheBalanceIsZero()
    {
        var gap = LivingGapCalculator.Measure(
            1000m,
            [],
            [new LivingDebtMinimum("Paid card", 0m, 125m, "USD")],
            [],
            "USD");

        Assert.Equal(0m, gap.MinimumsMonthly);
        Assert.Equal(0m, gap.Shortfall);
        Assert.Empty(gap.LeftOut);
    }
}
