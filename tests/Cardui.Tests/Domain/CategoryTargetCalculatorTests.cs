using Cardui.Api.Domain.CategoryTargets;
using Xunit;

namespace Cardui.Tests.Domain;

public class CategoryTargetCalculatorTests
{
    private static readonly Guid Groceries = Guid.NewGuid();
    private static readonly Guid Shopping = Guid.NewGuid();

    [Fact]
    public void Calculate_RollsThePreviousMonthOnlyWhenRolloverIsOn()
    {
        var earlier = new[]
        {
            History(2026, 8, Groceries, 100m, rollover: true, spent: 40m),
            History(2026, 9, Groceries, 100m, rollover: true, spent: 90m)
        };

        var october = CategoryTargetCalculator.Calculate(
            [new CategoryTargetAssignment(Groceries, 80m, Rollover: false)],
            earlier,
            [new CategoryMonthSpent(Groceries, 10m)],
            [Groceries],
            2026,
            10);

        var line = Assert.Single(october);
        Assert.Equal(80m, line.Target);
        Assert.False(line.Rollover);
        Assert.Equal(70m, line.RolloverIn);
        Assert.Equal(150m, line.Available);
        Assert.Equal(140m, line.Remaining);
    }

    [Fact]
    public void Calculate_DoesNotCarryWhenRolloverIsOff()
    {
        var earlier = new[]
        {
            History(2026, 9, Groceries, 100m, rollover: false, spent: 40m)
        };

        var line = Assert.Single(CategoryTargetCalculator.Calculate(
            [new CategoryTargetAssignment(Groceries, 100m, Rollover: false)],
            earlier,
            [new CategoryMonthSpent(Groceries, 0m)],
            [Groceries],
            2026,
            10));

        Assert.Equal(0m, line.RolloverIn);
        Assert.Equal(100m, line.Remaining);
    }

    [Fact]
    public void Calculate_ResetsTheCarryWhenAMonthIsMissing()
    {
        var earlier = new[]
        {
            History(2026, 8, Groceries, 100m, rollover: true, spent: 40m)
        };

        var line = Assert.Single(CategoryTargetCalculator.Calculate(
            [new CategoryTargetAssignment(Groceries, 100m, Rollover: true)],
            earlier,
            [],
            [Groceries],
            2026,
            10));

        Assert.Equal(0m, line.RolloverIn);
        Assert.Equal(100m, line.Remaining);
    }

    [Fact]
    public void Calculate_CarriesOverspendAsANegativeAmount()
    {
        var earlier = new[]
        {
            History(2026, 9, Groceries, 100m, rollover: true, spent: 130m)
        };

        var line = Assert.Single(CategoryTargetCalculator.Calculate(
            [new CategoryTargetAssignment(Groceries, 100m, Rollover: false)],
            earlier,
            [],
            [Groceries],
            2026,
            10));

        Assert.Equal(-30m, line.RolloverIn);
        Assert.Equal(70m, line.Remaining);
    }

    [Fact]
    public void Calculate_KeepsAKnownZeroAndLeavesAMissingTargetOpen()
    {
        var earlier = new[]
        {
            History(2026, 9, Shopping, 50m, rollover: true, spent: 10m)
        };

        var lines = CategoryTargetCalculator.Calculate(
            [new CategoryTargetAssignment(Groceries, 0m, Rollover: false)],
            earlier,
            [
                new CategoryMonthSpent(Groceries, 10m),
                new CategoryMonthSpent(Shopping, 5m),
                new CategoryMonthSpent(null, 15m)
            ],
            [Groceries, Shopping],
            2026,
            10);

        Assert.Equal(3, lines.Count);
        Assert.Equal(0m, lines[0].Target);
        Assert.Equal(-10m, lines[0].Remaining);
        Assert.Null(lines[1].Target);
        Assert.Equal(40m, lines[1].RolloverIn);
        Assert.Null(lines[1].Remaining);
        Assert.Equal(5m, lines[1].Spent);
        Assert.Null(lines[2].CategoryId);
        Assert.Equal(15m, lines[2].Spent);
        Assert.Null(lines[2].Target);
    }

    [Fact]
    public void CopyForward_CopiesAmountsAndChoicesWithoutLeftoverMoney()
    {
        var copied = CategoryTargetCalculator.CopyForward(
        [
            new CategoryTargetAssignment(Groceries, 100m, Rollover: true),
            new CategoryTargetAssignment(Groceries, 80m, Rollover: false)
        ]);

        var assignment = Assert.Single(copied);
        Assert.Equal(100m, assignment.Amount);
        Assert.True(assignment.Rollover);

        var line = Assert.Single(CategoryTargetCalculator.Calculate(
            copied,
            [History(2026, 9, Groceries, 100m, rollover: true, spent: 40m)],
            [],
            [Groceries],
            2026,
            10));

        Assert.Equal(100m, line.Target);
        Assert.Equal(60m, line.RolloverIn);
        Assert.Equal(160m, line.Available);
    }

    [Fact]
    public void SpendingEnd_StopsTheCurrentMonthTodayAndLeavesAFutureMonthUnstarted()
    {
        var today = new DateOnly(2026, 10, 5);

        Assert.Equal(new DateOnly(2026, 10, 5), CategoryTargetCalculator.SpendingEnd(2026, 10, today));
        Assert.Equal(new DateOnly(2026, 9, 30), CategoryTargetCalculator.SpendingEnd(2026, 9, today));
        Assert.Equal(today, CategoryTargetCalculator.SpendingEnd(2026, 11, today));
        Assert.Equal(
            (new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31)),
            CategoryTargetCalculator.MonthBounds(2026, 10));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(12.5)]
    public void TryReadAmount_AcceptsZeroAndCents(decimal amount)
    {
        Assert.True(CategoryTargetRules.TryReadAmount(amount, out var error));
        Assert.Equal("", error);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(10.001)]
    [InlineData(100_000_000.01)]
    public void TryReadAmount_RejectsANegativeFractionOrHugeAmount(decimal amount)
    {
        Assert.False(CategoryTargetRules.TryReadAmount(amount, out _));
    }

    #region Private Methods

    /// <summary>
    /// One earlier month with a single category target and its spent amount.
    /// </summary>
    private static CategoryTargetHistoryMonth History(
        int year,
        int month,
        Guid categoryId,
        decimal amount,
        bool rollover,
        decimal spent)
    {
        return new CategoryTargetHistoryMonth(
            year,
            month,
            [new CategoryTargetAssignment(categoryId, amount, rollover)],
            [new CategoryMonthSpent(categoryId, spent)]);
    }

    #endregion
}
