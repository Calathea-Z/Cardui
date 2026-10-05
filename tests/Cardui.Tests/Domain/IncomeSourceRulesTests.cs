using Cardui.Api.Domain;
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
        DateOnly? nextPaymentDate = null)
    {
        var ok = IncomeSourceRules.TryNormalize(
            "Paycheck",
            100m,
            cadence,
            nextPaymentDate ?? new DateOnly(2026, 10, 16),
            null,
            reliability,
            out _,
            out var error);
        return (ok, error);
    }
}
