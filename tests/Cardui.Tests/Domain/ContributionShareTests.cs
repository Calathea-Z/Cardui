using Cardui.Api.Domain.Living;
using Cardui.Api.Domain.Recovery;
using Cardui.Api.Models;
using Xunit;

namespace Cardui.Tests.Domain;

public class ContributionShareTests
{
    private static readonly Guid AlexId = Guid.Parse("91000000-0000-0000-0000-000000000001");
    private static readonly Guid JordanId = Guid.Parse("91000000-0000-0000-0000-000000000002");
    private static readonly Guid AlexPayId = Guid.Parse("91000000-0000-0000-0000-000000000011");
    private static readonly Guid JordanPayId = Guid.Parse("91000000-0000-0000-0000-000000000012");
    private static readonly DateOnly NextPay = new(2026, 10, 16);

    [Fact]
    public void Apply_ScalesScheduledPayAndLeavesUnassignedPayWhole()
    {
        var incomes = new[]
        {
            Pay(AlexPayId, "Alex pay", 4000m, IncomeCadence.Monthly, AlexId),
            Pay(JordanPayId, "Jordan pay", 600m, IncomeCadence.Biweekly, JordanId),
            Pay(Guid.Parse("91000000-0000-0000-0000-000000000013"), "Side", 1000m, IncomeCadence.Monthly, null)
        };

        var share = ContributionShare.Apply(
            incomes,
            [
                new ContributionShareInput(AlexId, "Alex", 2000m),
                new ContributionShareInput(JordanId, "Jordan", null)
            ],
            "USD");

        Assert.Equal(2000m, share.Incomes.Single(income => income.Id == AlexPayId).TypicalAmount);
        Assert.Equal(600m, share.Incomes.Single(income => income.Id == JordanPayId).TypicalAmount);
        Assert.Equal(1000m, share.Incomes.Single(income => income.Name == "Side").TypicalAmount);
        Assert.Equal(["Side"], share.UnassignedNames);
        Assert.Equal(2000m + 1300m + 1000m, share.SharedMonthly);

        var alex = share.People.Single(person => person.ContributorId == AlexId);
        Assert.Equal(ContributionLimit.Shared, alex.Limit);
        Assert.Equal(4000m, alex.RecordedMonthly);
        Assert.Equal(2000m, alex.SharedMonthly);
        Assert.Equal(2000m, alex.KeptMonthly);

        var jordan = share.People.Single(person => person.ContributorId == JordanId);
        Assert.Equal(ContributionLimit.FullPay, jordan.Limit);
        Assert.Equal(1300m, jordan.RecordedMonthly);
        Assert.Equal(0m, jordan.KeptMonthly);
    }

    [Fact]
    public void Apply_UsesAllRecordedPayWhenTheCapCoversItAndDoesNotInventADeposit()
    {
        var covered = ContributionShare.Apply(
            [Pay(AlexPayId, "Alex pay", 4000m, IncomeCadence.Monthly, AlexId)],
            [new ContributionShareInput(AlexId, "Alex", 5000m)],
            "USD");
        var unplaced = ContributionShare.Apply(
            [],
            [new ContributionShareInput(JordanId, "Jordan", 800m)],
            "USD");

        Assert.Equal(ContributionLimit.AllRecordedPay, covered.People[0].Limit);
        Assert.Equal(4000m, covered.Incomes[0].TypicalAmount);
        Assert.Equal(ContributionLimit.Unplaced, unplaced.People[0].Limit);
        Assert.Empty(unplaced.Incomes);
        Assert.Equal(0m, unplaced.SharedMonthly);
    }

    [Fact]
    public void Apply_KeepsTheSameShareOfARaiseAndOfLowPay()
    {
        var income = Pay(AlexPayId, "Alex pay", 4000m, IncomeCadence.Monthly, AlexId) with
        {
            LowAmount = 3000m,
            Raises = [new DatedIncomeRaise(new DateOnly(2027, 1, 1), 5000m)]
        };

        var share = ContributionShare.Apply(
            [income],
            [new ContributionShareInput(AlexId, "Alex", 1000m)],
            "USD");

        var scaled = share.Incomes[0];
        Assert.Equal(1000m, scaled.TypicalAmount);
        Assert.Equal(750m, scaled.LowAmount);
        Assert.Equal(1250m, scaled.Raises[0].Amount);
        Assert.Equal(NextPay, scaled.NextPaymentDate);
    }

    [Fact]
    public void Apply_LeavesAZeroCapAsAKnownZeroAndLeavesIrregularPayOnItsDate()
    {
        var irregular = Pay(AlexPayId, "Gift", 500m, IncomeCadence.Irregular, AlexId);
        var share = ContributionShare.Apply(
            [irregular],
            [new ContributionShareInput(AlexId, "Alex", 0m)],
            "USD");

        Assert.Equal(ContributionLimit.Unplaced, share.People[0].Limit);
        Assert.True(share.People[0].HasUnscheduledPay);
        Assert.Equal(500m, share.Incomes[0].TypicalAmount);
        Assert.Equal(NextPay, share.Incomes[0].NextPaymentDate);
    }

    private static HouseholdIncome Pay(
        Guid id,
        string name,
        decimal amount,
        IncomeCadence cadence,
        Guid? contributorId)
    {
        return new HouseholdIncome(id, name, "USD", amount, null, cadence, NextPay, [], contributorId);
    }
}
