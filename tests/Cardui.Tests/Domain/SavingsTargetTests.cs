using Cardui.Api.Domain.Recovery;
using Xunit;

namespace Cardui.Tests.Domain;

public class SavingsTargetTests
{
    [Fact]
    public void Plan_SplitsAnEvenGapAcrossTheMonthsThroughTheDate()
    {
        var plan = SavingsTarget.Plan(
            40m,
            100m,
            new DateOnly(2026, 1, 15),
            new DateOnly(2026, 4, 15),
            null);

        Assert.Equal(60m, plan.Remaining);
        Assert.False(plan.AlreadyMet);
        Assert.Equal(15m, plan.AmountNeededPerMonth);
        Assert.Equal(15m, plan.FinalAmountNeeded);
        Assert.Equal(
            [
                new SavingsContribution(new DateOnly(2026, 1, 15), 15m),
                new SavingsContribution(new DateOnly(2026, 2, 15), 15m),
                new SavingsContribution(new DateOnly(2026, 3, 15), 15m),
                new SavingsContribution(new DateOnly(2026, 4, 15), 15m)
            ],
            plan.ToHitDate);
        Assert.Empty(plan.FromContribution);
    }

    [Fact]
    public void Plan_TruesUpTheLastMonth()
    {
        var plan = SavingsTarget.Plan(
            0m,
            100m,
            new DateOnly(2026, 1, 31),
            new DateOnly(2026, 3, 31),
            null);

        Assert.Equal(33.33m, plan.AmountNeededPerMonth);
        Assert.Equal(33.34m, plan.FinalAmountNeeded);
        Assert.Equal([33.33m, 33.33m, 33.34m], plan.ToHitDate.Select(item => item.Amount));
        Assert.Equal(new DateOnly(2026, 2, 28), plan.ToHitDate[1].Date);
        Assert.Equal(100m, plan.ToHitDate.Sum(item => item.Amount));
    }

    [Fact]
    public void Plan_ReachesOnTheMonthTheContributionFinishes()
    {
        var plan = SavingsTarget.Plan(
            0m,
            60m,
            new DateOnly(2026, 1, 31),
            new DateOnly(2026, 6, 30),
            25m);

        Assert.Equal(new DateOnly(2026, 3, 31), plan.ContributionReachesOn);
        Assert.Equal([25m, 25m, 10m], plan.FromContribution.Select(item => item.Amount));
        Assert.False(plan.ContributionDoesNotReach);
        Assert.Equal(10m, plan.AmountNeededPerMonth);
    }

    [Fact]
    public void Plan_ReportsATargetThatIsAlreadyReserved()
    {
        var plan = SavingsTarget.Plan(
            150m,
            100m,
            new DateOnly(2026, 1, 15),
            new DateOnly(2026, 4, 15),
            25m);

        Assert.True(plan.AlreadyMet);
        Assert.Equal(0m, plan.Remaining);
        Assert.Empty(plan.ToHitDate);
        Assert.Empty(plan.FromContribution);
    }

    [Fact]
    public void Plan_ReportsAPastDateAsDueAtTheStart()
    {
        var plan = SavingsTarget.Plan(
            0m,
            40m,
            new DateOnly(2026, 1, 15),
            new DateOnly(2025, 12, 1),
            null);

        Assert.True(plan.DatePassed);
        var due = Assert.Single(plan.ToHitDate);
        Assert.Equal(new DateOnly(2026, 1, 15), due.Date);
        Assert.Equal(40m, due.Amount);
    }

    [Fact]
    public void Plan_DoesNotInventAReachDateForAZeroContribution()
    {
        var plan = SavingsTarget.Plan(
            0m,
            100m,
            new DateOnly(2026, 1, 15),
            new DateOnly(2026, 4, 15),
            0m);

        Assert.Null(plan.ContributionReachesOn);
        Assert.False(plan.ContributionDoesNotReach);
        Assert.Empty(plan.FromContribution);
        Assert.Equal(4, plan.ToHitDate.Count);
    }

    [Fact]
    public void Plan_DoesNotSpreadATargetPastTheMonthCap()
    {
        var from = new DateOnly(2026, 1, 15);
        var plan = SavingsTarget.Plan(
            0m,
            100m,
            from,
            from.AddMonths(SavingsTarget.MaxMonths),
            null);

        Assert.True(plan.TargetDateBeyondHorizon);
        Assert.Empty(plan.ToHitDate);
        Assert.Null(plan.AmountNeededPerMonth);
        Assert.Equal(100m, plan.Remaining);
    }

    [Fact]
    public void Plan_StopsAContributionThatCannotFinishWithinTheHorizon()
    {
        var plan = SavingsTarget.Plan(
            0m,
            1000m,
            new DateOnly(2026, 1, 15),
            null,
            1m);

        Assert.True(plan.ContributionDoesNotReach);
        Assert.Null(plan.ContributionReachesOn);
        Assert.Empty(plan.FromContribution);
        Assert.Equal(1000m, plan.Remaining);
    }
}
