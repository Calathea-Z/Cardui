using System.Globalization;
using Cardui.Api.Domain.Debts;
using Cardui.Api.Models;
using Xunit;

namespace Cardui.Tests.Domain;

public class DebtRulesTests
{
    [Fact]
    public void TryNormalize_KeepsARevolvingDebtAndLeavesUnknownTermsEmpty()
    {
        var accountId = Guid.NewGuid();
        var ok = DebtRules.TryNormalize(
            "  Store card  ",
            DebtKind.Revolving,
            accountId,
            842.5m,
            new DateOnly(2026, 10, 1),
            null,
            null,
            null,
            null,
            36,
            null,
            null,
            out var draft,
            out var error);

        Assert.True(ok);
        Assert.Equal("", error);
        Assert.Equal("Store card", draft.Name);
        Assert.Equal(DebtKind.Revolving, draft.Kind);
        Assert.Equal(accountId, draft.AccountId);
        Assert.Equal(842.5m, draft.Balance);
        Assert.Equal(new DateOnly(2026, 10, 1), draft.BalanceAsOf);
        Assert.Null(draft.Apr);
        Assert.Null(draft.MinimumPayment);
        Assert.Null(draft.NextDueDate);
        Assert.Null(draft.CreditLimit);
        Assert.Null(draft.RemainingTermMonths);
        Assert.Null(draft.PromotionalApr);
        Assert.Null(draft.PromotionalEndsOn);
        Assert.Null(DebtRules.Utilization(draft.Balance, draft.CreditLimit));
    }

    [Fact]
    public void TryNormalize_KeepsInstallmentTermsAndDropsACreditLimit()
    {
        var ok = DebtRules.TryNormalize(
            "Car loan",
            DebtKind.Installment,
            Guid.Empty,
            8420m,
            new DateOnly(2026, 9, 15),
            6.5m,
            250m,
            new DateOnly(2026, 11, 1),
            5000m,
            36,
            0m,
            new DateOnly(2027, 4, 1),
            out var draft,
            out var error);

        Assert.True(ok);
        Assert.Equal("", error);
        Assert.Null(draft.AccountId);
        Assert.Equal(6.5m, draft.Apr);
        Assert.Equal(250m, draft.MinimumPayment);
        Assert.Equal(new DateOnly(2026, 11, 1), draft.NextDueDate);
        Assert.Null(draft.CreditLimit);
        Assert.Equal(36, draft.RemainingTermMonths);
        Assert.Equal(0m, draft.PromotionalApr);
        Assert.Equal(new DateOnly(2027, 4, 1), draft.PromotionalEndsOn);
        Assert.Null(DebtRules.Utilization(draft.Balance, draft.CreditLimit));
    }

    [Fact]
    public void TryNormalize_KeepsAKnownZeroAndARevolvingLimit()
    {
        var ok = DebtRules.TryNormalize(
            "Card",
            DebtKind.Revolving,
            null,
            0m,
            new DateOnly(2026, 10, 1),
            0m,
            0m,
            null,
            1000m,
            null,
            null,
            null,
            out var draft,
            out var error);

        Assert.True(ok);
        Assert.Equal("", error);
        Assert.Equal(0m, draft.Balance);
        Assert.Equal(0m, draft.Apr);
        Assert.Equal(0m, draft.MinimumPayment);
        Assert.Equal(1000m, draft.CreditLimit);
        Assert.Equal(0m, DebtRules.Utilization(draft.Balance, draft.CreditLimit));
    }

    [Fact]
    public void Utilization_UsesTheBalanceAndLimitAndStaysUnknownWhenEitherIsMissing()
    {
        Assert.Equal(0.842m, DebtRules.Utilization(842m, 1000m));
        Assert.Equal(1.5m, DebtRules.Utilization(1500m, 1000m));
        Assert.Null(DebtRules.Utilization(null, 1000m));
        Assert.Null(DebtRules.Utilization(842m, null));
        Assert.Null(DebtRules.Utilization(842m, 0m));
    }

    [Theory]
    [InlineData(null, "2026-10-01", "Enter the balance, or clear the date.")]
    [InlineData("10", null, "Enter the date this balance was true.")]
    [InlineData("-1", "2026-10-01", "Enter the balance in dollars and cents, or leave it blank.")]
    [InlineData("10.125", "2026-10-01", "Enter the amount in dollars and cents.")]
    public void TryNormalize_RejectsABalanceThatIsNotDated(
        string? balance,
        string? balanceAsOf,
        string message)
    {
        var ok = DebtRules.TryNormalize(
            "Card",
            DebtKind.Revolving,
            null,
            balance is null ? null : decimal.Parse(balance, CultureInfo.InvariantCulture),
            balanceAsOf is null ? null : DateOnly.Parse(balanceAsOf),
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            out _,
            out var error);

        Assert.False(ok);
        Assert.Equal(message, error);
    }

    [Fact]
    public void TryNormalize_RejectsAMissingChoiceOrATermOutsideTheRange()
    {
        Assert.Equal("A debt name is required.", Reject(name: "  ").error);
        Assert.Equal("Choose revolving or installment.", Reject(kind: null).error);
        Assert.Equal(
            "Enter the APR as a percent, or leave it blank.",
            Reject(apr: -1m).error);
        Assert.Equal(
            "Enter the APR with up to three decimal places.",
            Reject(apr: 19.9991m).error);
        Assert.Equal("That APR is too large.", Reject(apr: DebtRules.MaxApr + 0.001m).error);
        Assert.Equal(
            "Enter the credit limit, or leave it blank.",
            Reject(creditLimit: 0m).error);
        Assert.Equal(
            "Enter the months left, or leave the term blank.",
            Reject(kind: DebtKind.Installment, remainingTermMonths: 0).error);
        Assert.Equal(
            "Enter the due date.",
            Reject(nextDueDate: new DateOnly(1999, 12, 31)).error);
        Assert.Equal(
            "That amount is too large.",
            Reject(balance: DebtRules.MaxAmount + 0.01m, balanceAsOf: new DateOnly(2026, 10, 1)).error);
    }

    [Fact]
    public void TryNormalize_AllowsAPromotionWithOnlyOneFact()
    {
        var ok = DebtRules.TryNormalize(
            "Card",
            DebtKind.Revolving,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            0m,
            null,
            out var draft,
            out var error);

        Assert.True(ok);
        Assert.Equal("", error);
        Assert.Equal(0m, draft.PromotionalApr);
        Assert.Null(draft.PromotionalEndsOn);
        Assert.Null(draft.Balance);
        Assert.Null(draft.BalanceAsOf);
    }

    [Fact]
    public void TryReadBalanceOverride_RequiresAnAmountAndUsesTodayWhenTheDateIsMissing()
    {
        var today = new DateOnly(2026, 10, 7);
        var dated = DebtRules.TryReadBalanceOverride(
            1240.18m,
            new DateOnly(2026, 10, 2),
            today,
            out var amount,
            out var asOf,
            out var error);

        Assert.True(dated);
        Assert.Equal("", error);
        Assert.Equal(1240.18m, amount);
        Assert.Equal(new DateOnly(2026, 10, 2), asOf);

        var omitted = DebtRules.TryReadBalanceOverride(
            0m,
            null,
            today,
            out amount,
            out asOf,
            out error);
        Assert.True(omitted);
        Assert.Equal(0m, amount);
        Assert.Equal(today, asOf);

        var blank = DebtRules.TryReadBalanceOverride(null, today, today, out _, out _, out error);
        Assert.False(blank);
        Assert.Equal("Enter the balance.", error);
    }

    private static (bool ok, string error) Reject(
        string name = "Card",
        DebtKind? kind = DebtKind.Revolving,
        decimal? apr = null,
        decimal? creditLimit = null,
        int? remainingTermMonths = null,
        DateOnly? nextDueDate = null,
        decimal? balance = null,
        DateOnly? balanceAsOf = null)
    {
        var ok = DebtRules.TryNormalize(
            name,
            kind,
            null,
            balance,
            balanceAsOf,
            apr,
            null,
            nextDueDate,
            creditLimit,
            remainingTermMonths,
            null,
            null,
            out _,
            out var error);
        return (ok, error);
    }
}
