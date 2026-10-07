using Cardui.Api.Domain.Income;
using Cardui.Api.Models;
using Xunit;

namespace Cardui.Tests.Domain;

public class IncomeSourceRulesTests
{
    [Fact]
    public void TryNormalize_TrimsTheNameAndKeepsOnePayment()
    {
        var ok = IncomeSourceRules.TryNormalize(
            "  Paycheck  ",
            2400.5m,
            IncomeCadence.Biweekly,
            new DateOnly(2026, 10, 16),
            Guid.Empty,
            IncomeReliability.Steady,
            out var draft,
            out var error);

        Assert.True(ok);
        Assert.Equal("", error);
        Assert.Equal("Paycheck", draft.Name);
        Assert.Equal(2400.5m, draft.TakeHomeAmount);
        Assert.Equal(IncomeCadence.Biweekly, draft.Cadence);
        Assert.Equal(new DateOnly(2026, 10, 16), draft.NextPaymentDate);
        Assert.Null(draft.ContributorId);
        Assert.Equal(IncomeReliability.Steady, draft.Reliability);
        Assert.Null(draft.LowTakeHomeAmount);
        Assert.Null(draft.StrongTakeHomeAmount);
        Assert.Null(draft.GrossPayAmount);
        Assert.Empty(draft.Raises);
    }

    [Fact]
    public void TryNormalize_KeepsScenariosAndOrdersRaisesWithoutChangingTypical()
    {
        var ok = IncomeSourceRules.TryNormalize(
            "Paycheck",
            2400.50m,
            IncomeCadence.Biweekly,
            new DateOnly(2026, 10, 16),
            null,
            IncomeReliability.Variable,
            out var draft,
            out var error,
            lowTakeHomeAmount: 1800m,
            strongTakeHomeAmount: 3000m,
            raises:
            [
                new IncomeRaiseDraft(new DateOnly(2027, 1, 1), 2600m),
                new IncomeRaiseDraft(new DateOnly(2026, 10, 16), 2500m)
            ]);

        Assert.True(ok);
        Assert.Equal("", error);
        Assert.Equal(2400.50m, draft.TakeHomeAmount);
        Assert.Equal(1800m, draft.LowTakeHomeAmount);
        Assert.Equal(3000m, draft.StrongTakeHomeAmount);
        Assert.Equal(new DateOnly(2026, 10, 16), draft.Raises[0].EffectiveDate);
        Assert.Equal(2500m, draft.Raises[0].TakeHomeAmount);
        Assert.Equal(new DateOnly(2027, 1, 1), draft.Raises[1].EffectiveDate);
    }

    [Fact]
    public void TryNormalize_RejectsAScenarioOnTheWrongSideOfTypical()
    {
        Assert.Equal(
            "Low net pay cannot be higher than the typical amount.",
            Reject(lowTakeHomeAmount: 101m).error);
        Assert.Equal(
            "Strong net pay cannot be lower than the typical amount.",
            Reject(strongTakeHomeAmount: 99m).error);
        Assert.Equal(
            "Enter the low net pay in dollars and cents.",
            Reject(lowTakeHomeAmount: 10.125m).error);
        Assert.Equal(
            "Gross pay cannot be lower than the typical net pay.",
            Reject(grossPayAmount: 99m).error);
        Assert.Equal(
            "Enter the gross pay for one payment.",
            Reject(grossPayAmount: 0m).error);
    }

    [Fact]
    public void TryNormalize_KeepsGrossPayForTheSamePayment()
    {
        var ok = IncomeSourceRules.TryNormalize(
            "Paycheck",
            2400.50m,
            IncomeCadence.Biweekly,
            new DateOnly(2026, 10, 16),
            null,
            IncomeReliability.Steady,
            out var draft,
            out var error,
            grossPayAmount: 3200m);

        Assert.True(ok);
        Assert.Equal("", error);
        Assert.Equal(2400.50m, draft.TakeHomeAmount);
        Assert.Equal(3200m, draft.GrossPayAmount);
    }

    [Fact]
    public void TryNormalize_RejectsARaiseBeforeTheNextPaymentOrOnARepeatedDate()
    {
        Assert.Equal(
            "Enter a raise date on or after the next payment.",
            Reject(raises: [new IncomeRaiseDraft(new DateOnly(2026, 10, 15), 120m)]).error);
        Assert.Equal(
            "Each expected raise needs its own date.",
            Reject(raises:
            [
                new IncomeRaiseDraft(new DateOnly(2026, 11, 1), 120m),
                new IncomeRaiseDraft(new DateOnly(2026, 11, 1), 130m)
            ]).error);
        Assert.Equal(
            "Enter the raise date.",
            Reject(raises: [new IncomeRaiseDraft(new DateOnly(1999, 1, 1), 120m)]).error);
        Assert.Equal(
            "A raise cannot be lower than the typical net pay.",
            Reject(raises: [new IncomeRaiseDraft(new DateOnly(2026, 11, 1), 99m)]).error);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void TryNormalize_RejectsABlankName(string name)
    {
        var ok = IncomeSourceRules.TryNormalize(
            name,
            100m,
            IncomeCadence.Monthly,
            new DateOnly(2026, 10, 16),
            null,
            IncomeReliability.Variable,
            out _,
            out var error);

        Assert.False(ok);
        Assert.Equal("An income source name is required.", error);
    }

    [Fact]
    public void TryNormalize_RejectsANameOver80Characters()
    {
        var ok = IncomeSourceRules.TryNormalize(
            new string('a', 81),
            100m,
            IncomeCadence.Monthly,
            new DateOnly(2026, 10, 16),
            null,
            IncomeReliability.Variable,
            out _,
            out var error);

        Assert.False(ok);
        Assert.Equal("An income source name must be 80 characters or fewer.", error);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    public void TryNormalize_RejectsATakeHomeThatIsNotAPayment(string amountText)
    {
        var ok = IncomeSourceRules.TryNormalize(
            "Paycheck",
            decimal.Parse(amountText),
            IncomeCadence.Weekly,
            new DateOnly(2026, 10, 16),
            null,
            IncomeReliability.Steady,
            out _,
            out var error);

        Assert.False(ok);
        Assert.Equal("Enter the net pay for one payment.", error);
    }

    [Fact]
    public void TryNormalize_RejectsAnAmountWithFractionalCents()
    {
        var ok = IncomeSourceRules.TryNormalize(
            "Paycheck",
            10.125m,
            IncomeCadence.Weekly,
            new DateOnly(2026, 10, 16),
            null,
            IncomeReliability.Steady,
            out _,
            out var error);

        Assert.False(ok);
        Assert.Equal("Enter the net pay in dollars and cents.", error);
    }

    [Fact]
    public void TryNormalize_RejectsAMissingCadenceReliabilityOrDate()
    {
        Assert.Equal(
            "Choose how often this income is paid.",
            Reject(cadence: null).error);
        Assert.Equal(
            "Choose how reliable this income is.",
            Reject(reliability: null).error);
        Assert.Equal(
            "Enter the next payment date.",
            Reject(nextPaymentDate: new DateOnly(1999, 12, 31)).error);
    }

    private static (bool ok, string error) Reject(
        IncomeCadence? cadence = IncomeCadence.Monthly,
        IncomeReliability? reliability = IncomeReliability.Uncertain,
        DateOnly? nextPaymentDate = null,
        decimal? lowTakeHomeAmount = null,
        decimal? strongTakeHomeAmount = null,
        IReadOnlyList<IncomeRaiseDraft>? raises = null,
        decimal? grossPayAmount = null)
    {
        var ok = IncomeSourceRules.TryNormalize(
            "Paycheck",
            100m,
            cadence,
            nextPaymentDate ?? new DateOnly(2026, 10, 16),
            null,
            reliability,
            out _,
            out var error,
            lowTakeHomeAmount,
            strongTakeHomeAmount,
            raises,
            grossPayAmount);
        return (ok, error);
    }
}
