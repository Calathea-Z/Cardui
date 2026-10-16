using Cardui.Api.Domain.Recovery;
using Cardui.Api.Models;
using Xunit;

namespace Cardui.Tests.Domain;

public class RestructuringComparisonTests
{
    private static readonly DateOnly AsOf = new(2026, 1, 1);

    [Fact]
    public void Compare_UsesTheFullBalanceAndDoesNotRankALowerPaymentThatCostsMore()
    {
        var compared = Compare(
            [Loan("Card", 1200m, 0m, 100m, new DateOnly(2026, 1, 1))],
            Offer(RestructuringOfferKind.ReplacementLoan, 12m, 24));

        Assert.True(compared.Accepted);
        var result = compared.Result!;
        Assert.Equal(1200m, result.CoveredBalance);
        Assert.Equal(1200m, result.NewPrincipal);
        Assert.Equal(56.49m, result.OfferMonthlyPayment);
        Assert.Equal(100m, result.KeepMonthlyPayment);
        Assert.Equal(43.51m, result.MonthlyCashFlow);
        Assert.Equal(0m, result.KeepTotalInterest);
        Assert.True(result.OfferTotalInterest > 0);
        Assert.True(result.OfferTotalCost > result.KeepTotalCost);
        Assert.Equal(RestructuringBreakEven.Never, result.BreakEven);
        Assert.Null(result.BreakEvenOn);
    }

    [Fact]
    public void Compare_FindsTheBreakEvenDateForABalanceTransferFee()
    {
        var compared = Compare(
            [Card("Card", 1000m, 18m, 100m, new DateOnly(2026, 2, 1))],
            Offer(RestructuringOfferKind.BalanceTransfer, 0m, 12, fee: 30m));

        Assert.True(compared.Accepted);
        var result = compared.Result!;
        Assert.Equal(1030m, result.NewPrincipal);
        Assert.Equal(0m, result.CashDueNow);
        Assert.Equal(30m, result.OfferTotalCost);
        Assert.Equal(85.83m, result.OfferMonthlyPayment);
        Assert.True(result.KeepTotalInterest >= 30m);
        Assert.Equal(RestructuringBreakEven.Reached, result.BreakEven);
        Assert.Equal(new DateOnly(2026, 4, 1), result.BreakEvenOn);
        Assert.True(result.MonthlyCashFlow > 0);
        Assert.True(result.OfferTotalCost < result.KeepTotalCost);
    }

    [Fact]
    public void Compare_TreatsACashFeeAsUpfrontCashAndAFinancedFeeAsPrincipal()
    {
        var financed = Compare(
            [Loan("Loan", 1000m, 0m, 100m, new DateOnly(2026, 1, 1))],
            Offer(RestructuringOfferKind.ReplacementLoan, 0m, 10, fee: 30m));
        var paid = Compare(
            [Loan("Loan", 1000m, 0m, 100m, new DateOnly(2026, 1, 1))],
            Offer(
                RestructuringOfferKind.ReplacementLoan,
                0m,
                10,
                fee: 30m,
                funding: RestructuringFeeFunding.Cash),
            new RestructuringCash(1000m, 400m, 500m, true));

        Assert.Equal(1030m, financed.Result!.NewPrincipal);
        Assert.Equal(0m, financed.Result.CashDueNow);
        Assert.Equal(30m, financed.Result.OfferTotalCost);
        Assert.Equal(1000m, paid.Result!.NewPrincipal);
        Assert.Equal(30m, paid.Result.CashDueNow);
        Assert.Equal(30m, paid.Result.OfferTotalCost);
        Assert.False(paid.Result.Cash.ExceedsCashOnHand);
        Assert.False(paid.Result.Cash.UsesReserve);
        Assert.False(paid.Result.Cash.LeavesLessThanCashToKeep);
    }

    [Fact]
    public void Compare_ReportsWhenUpfrontCashPassesCashOnHand()
    {
        var compared = Compare(
            [Loan("Loan", 1000m, 0m, 100m, new DateOnly(2026, 1, 1))],
            Offer(RestructuringOfferKind.ReplacementLoan, 0m, 10, upfront: 200m),
            new RestructuringCash(100m, 0m, 0m, false));

        Assert.True(compared.Accepted);
        Assert.Equal(800m, compared.Result!.NewPrincipal);
        Assert.Equal(200m, compared.Result.CashDueNow);
        Assert.True(compared.Result.Cash.ExceedsCashOnHand);
    }

    [Fact]
    public void Compare_MatchesTwoDebtsWhenTheConsolidationAddsNoCost()
    {
        var compared = Compare(
            [
                Loan("First", 400m, 0m, 40m, new DateOnly(2026, 5, 1)),
                Loan("Second", 600m, 0m, 60m, new DateOnly(2026, 5, 1))
            ],
            Offer(RestructuringOfferKind.ConsolidationLoan, 0m, 10));

        Assert.True(compared.Accepted);
        var result = compared.Result!;
        Assert.Equal(1000m, result.CoveredBalance);
        Assert.Equal(100m, result.OfferMonthlyPayment);
        Assert.Equal(0m, result.MonthlyCashFlow);
        Assert.Equal(0m, result.KeepTotalCost);
        Assert.Equal(0m, result.OfferTotalCost);
        Assert.Equal(RestructuringBreakEven.Reached, result.BreakEven);
        Assert.Equal(new DateOnly(2026, 5, 1), result.BreakEvenOn);
    }

    [Fact]
    public void Compare_UsesAPromotionalRateOnTheCurrentPath()
    {
        var debt = new RestructuringDebt(
            Guid.NewGuid(),
            "Card",
            "USD",
            DebtKind.Revolving,
            300m,
            20m,
            0m,
            null,
            100m,
            null,
            new DateOnly(2026, 1, 1));
        var compared = Compare(
            [debt],
            Offer(RestructuringOfferKind.ReplacementLoan, 0m, 3));

        Assert.True(compared.Accepted);
        Assert.Equal(0m, compared.Result!.KeepTotalInterest);
        Assert.Equal(20m, compared.Result.Debts[0].Apr);
        Assert.Equal(0m, compared.Result.Debts[0].PromotionalApr);
    }

    [Fact]
    public void Compare_LeavesBreakEvenUnknownWhenTheCurrentDebtDoesNotPayDown()
    {
        var compared = Compare(
            [Card("Card", 1000m, 24m, 10m, new DateOnly(2026, 1, 1))],
            Offer(RestructuringOfferKind.BalanceTransfer, 0m, 12, fee: 30m));

        Assert.True(compared.Accepted);
        Assert.Null(compared.Result!.KeepTotalInterest);
        Assert.Null(compared.Result.KeepPaidOffOn);
        Assert.Equal(DebtScheduleStop.DoesNotPayDown, compared.Result.Debts[0].Stop);
        Assert.Equal(RestructuringBreakEven.Unknown, compared.Result.BreakEven);
        Assert.Null(compared.Result.BreakEvenOn);
    }

    [Fact]
    public void Compare_LeavesTheMonthlyChangeUnknownWhenTheMinimumIsMissing()
    {
        var compared = Compare(
            [Card("Card", 500m, 20m, null, new DateOnly(2026, 1, 1))],
            Offer(RestructuringOfferKind.ReplacementLoan, 10m, 12));

        Assert.True(compared.Accepted);
        Assert.Null(compared.Result!.KeepMonthlyPayment);
        Assert.Null(compared.Result.MonthlyCashFlow);
        Assert.Equal(RestructuringBreakEven.Unknown, compared.Result.BreakEven);
    }

    [Theory]
    [InlineData(RestructuringOfferKind.ConsolidationLoan, 1, "A consolidation loan replaces two or more debts. A single debt is a replacement loan.")]
    [InlineData(RestructuringOfferKind.ReplacementLoan, 2, "A replacement loan replaces one debt. Two or more debts are a consolidation loan.")]
    public void Compare_RejectsAKindThatDoesNotMatchTheDebtCount(
        RestructuringOfferKind kind,
        int count,
        string message)
    {
        var debts = Enumerable.Range(0, count)
            .Select(index => Loan($"Loan {index}", 100m, 0m, 50m, new DateOnly(2026, 1, 1)))
            .ToList();
        var compared = Compare(debts, Offer(kind, 0m, 6));

        Assert.False(compared.Accepted);
        Assert.Equal(message, compared.Error);
    }

    [Fact]
    public void Compare_RejectsABalanceTransferThatIncludesAnInstallmentLoan()
    {
        var compared = Compare(
            [Loan("Auto", 1000m, 6m, 50m, new DateOnly(2026, 1, 1))],
            Offer(RestructuringOfferKind.BalanceTransfer, 0m, 12));

        Assert.False(compared.Accepted);
        Assert.Equal(
            "A balance transfer can include only revolving debts. Auto is an installment loan.",
            compared.Error);
    }

    [Fact]
    public void Compare_RejectsAnotherCurrency()
    {
        var debt = Card("Trip", 400m, 10m, 25m, new DateOnly(2026, 1, 1));
        var compared = Compare(
            [debt with { Currency = "EUR" }],
            Offer(RestructuringOfferKind.BalanceTransfer, 0m, 12));

        Assert.False(compared.Accepted);
        Assert.Equal("Trip is in another currency, so it can't be part of this comparison.", compared.Error);
    }

    [Fact]
    public void Compare_RejectsUpfrontCashThatPaysTheBalanceOff()
    {
        var compared = Compare(
            [Loan("Loan", 1000m, 0m, 100m, new DateOnly(2026, 1, 1))],
            Offer(RestructuringOfferKind.ReplacementLoan, 0m, 10, upfront: 1000m));

        Assert.False(compared.Accepted);
        Assert.Equal(
            "Upfront cash pays these balances, so there is no loan or transfer left to compare.",
            compared.Error);
    }

    [Fact]
    public void Compare_RejectsAnAprAndAFeeThatAreNotUsable()
    {
        var debt = Loan("Loan", 1000m, 0m, 100m, new DateOnly(2026, 1, 1));
        var apr = Compare([debt], Offer(RestructuringOfferKind.ReplacementLoan, 1000m, 12));
        var fee = Compare(
            [debt],
            new RestructuringOffer(
                RestructuringOfferKind.ReplacementLoan,
                0m,
                12,
                1.234m,
                RestructuringFeeFunding.Financed,
                0m));

        Assert.Equal("That APR is too large.", apr.Error);
        Assert.Equal("Enter the fee in dollars and cents, or zero.", fee.Error);
    }

    #region Private Methods

    private static (bool Accepted, RestructuringComparison? Result, string Error) Compare(
        IReadOnlyList<RestructuringDebt> debts,
        RestructuringOffer offer,
        RestructuringCash? cash = null)
    {
        var accepted = RestructuringComparisonRules.TryCompare(
            "USD",
            AsOf,
            debts,
            offer,
            cash ?? new RestructuringCash(0m, 0m, 0m, false),
            out var comparison,
            out var error);
        return (accepted, comparison, error);
    }

    private static RestructuringOffer Offer(
        RestructuringOfferKind kind,
        decimal apr,
        int termMonths,
        decimal fee = 0m,
        RestructuringFeeFunding funding = RestructuringFeeFunding.Financed,
        decimal upfront = 0m)
    {
        return new RestructuringOffer(kind, apr, termMonths, fee, funding, upfront);
    }

    private static RestructuringDebt Loan(
        string name,
        decimal balance,
        decimal apr,
        decimal? minimum,
        DateOnly due)
    {
        return Debt(name, DebtKind.Installment, balance, apr, minimum, due);
    }

    private static RestructuringDebt Card(
        string name,
        decimal balance,
        decimal? apr,
        decimal? minimum,
        DateOnly? due)
    {
        return Debt(name, DebtKind.Revolving, balance, apr, minimum, due);
    }

    private static RestructuringDebt Debt(
        string name,
        DebtKind kind,
        decimal balance,
        decimal? apr,
        decimal? minimum,
        DateOnly? due)
    {
        return new RestructuringDebt(
            Guid.NewGuid(),
            name,
            "USD",
            kind,
            balance,
            apr,
            null,
            null,
            minimum,
            null,
            due);
    }

    #endregion
}
