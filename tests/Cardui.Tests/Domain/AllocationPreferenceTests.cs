using Cardui.Api.Domain.Allocation;
using Cardui.Api.Models;
using Xunit;

namespace Cardui.Tests.Domain;

public class AllocationPreferenceTests
{
    [Fact]
    public void TryNormalize_RejectsAPercentOutsideZeroToOneHundred()
    {
        var accepted = AllocationPreferenceRules.TryNormalize(0, 0, 100, 0, out var draft, out var error);

        Assert.True(accepted);
        Assert.NotNull(draft);
        Assert.Equal("", error);

        var rejected = AllocationPreferenceRules.TryNormalize(-1, 0, 0, 0, out var rejectedDraft, out var rejectedError);

        Assert.False(rejected);
        Assert.Null(rejectedDraft);
        Assert.Equal(
            "Variable income: enter a whole percent from 0 to 100 for extra debt.",
            rejectedError);
    }

    [Fact]
    public void TryNormalize_RejectsDebtAndSavingsThatPassOneHundred()
    {
        var rejected = AllocationPreferenceRules.TryNormalize(60, 50, 0, 0, out var draft, out var error);

        Assert.False(rejected);
        Assert.Null(draft);
        Assert.Equal(
            "Variable income: debt and savings together can't be more than 100 percent. The rest stays available.",
            error);
    }

    [Theory]
    [InlineData(0, 0, 100)]
    [InlineData(100, 0, 0)]
    [InlineData(40, 40, 20)]
    public void CashPercent_IsWhatStaysAvailable(int debt, int savings, int cash)
    {
        Assert.Equal(cash, AllocationPreferenceRules.CashPercent(debt, savings));
    }

    [Fact]
    public void Split_KeepsTheAmountAndRoundsCentsAwayFromZero()
    {
        var even = AllocationPreferenceRules.Split(10m, 50, 50);
        var thirds = AllocationPreferenceRules.Split(10m, 33, 33);
        var oneCent = AllocationPreferenceRules.Split(0.01m, 50, 50);
        var none = AllocationPreferenceRules.Split(100m, 0, 0);
        var empty = AllocationPreferenceRules.Split(0m, 40, 40);

        Assert.Equal(new AllocationSplit(5m, 5m, 0m), even);
        Assert.Equal(new AllocationSplit(3.30m, 3.30m, 3.40m), thirds);
        Assert.Equal(new AllocationSplit(0.01m, 0m, 0m), oneCent);
        Assert.Equal(new AllocationSplit(0m, 0m, 100m), none);
        Assert.Equal(new AllocationSplit(0m, 0m, 0m), empty);
        Assert.Equal(10m, even.Debt + even.Savings + even.Cash);
        Assert.Equal(10m, thirds.Debt + thirds.Savings + thirds.Cash);
        Assert.Equal(0.01m, oneCent.Debt + oneCent.Savings + oneCent.Cash);
    }

    [Fact]
    public void Review_SplitsOnlyAVariableSurplusInThePlanningCurrency()
    {
        var reviews = AllocationPreferenceRules.Review(
            [
                Payment("Snooze", "USD", IncomeReliability.Variable, 800m, 1000m),
                Payment("Tips", "USD", IncomeReliability.Variable, 100m, null),
                Payment("Side job", "EUR", IncomeReliability.Variable, 50m, 80m),
                Payment("Salary", "USD", IncomeReliability.Steady, 2000m, 2200m),
                Payment("Maybe", "USD", IncomeReliability.Uncertain, 40m, 90m)
            ],
            "USD");

        Assert.Equal(3, reviews.Count);
        Assert.Equal(new VariableIncomeReview("Snooze", "USD", 200m, null), reviews[0]);
        Assert.Equal(
            new VariableIncomeReview("Tips", "USD", null, AllocationPreferenceRules.NoStrongSurplus),
            reviews[1]);
        Assert.Equal(
            new VariableIncomeReview("Side job", "EUR", null, AllocationPreferenceRules.OtherCurrency),
            reviews[2]);
    }

    private static VariableIncomePayment Payment(
        string name,
        string currency,
        IncomeReliability reliability,
        decimal typical,
        decimal? strong)
    {
        return new VariableIncomePayment(name, currency, reliability, typical, strong);
    }
}
