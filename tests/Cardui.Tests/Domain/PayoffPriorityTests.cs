using Cardui.Api.Domain.Recovery;
using Cardui.Api.Models;
using Xunit;

namespace Cardui.Tests.Domain;

public class PayoffPriorityTests
{
    private static readonly Guid StoreId = Guid.Parse("30000000-0000-0000-0000-000000000001");
    private static readonly Guid CardId = Guid.Parse("30000000-0000-0000-0000-000000000002");
    private static readonly Guid VisaId = Guid.Parse("30000000-0000-0000-0000-000000000003");
    private static readonly DateOnly Due = new(2026, 1, 15);

    [Fact]
    public void Compare_RemovesAMinimumWhenTheCashKeptExceedsTheExtraInterest()
    {
        var comparison = PayoffPriority.Compare(Input(
            100m,
            [
                Debt(StoreId, "Store", 100m, 10m, 25m, limit: 1000m),
                Debt(CardId, "Card", 500m, 20m, 25m, limit: 2000m)
            ]));

        Assert.Equal([CardId, StoreId], comparison.Avalanche.DebtIds);
        Assert.Equal([StoreId, CardId], comparison.Recommended.DebtIds);
        Assert.Equal(comparison.Avalanche.DebtIds, comparison.UserSelected.DebtIds);
        Assert.False(comparison.UserOrderProvided);
        Assert.Equal(24.02m, comparison.Avalanche.TotalInterest);
        Assert.Equal(27.91m, comparison.Recommended.TotalInterest);
        Assert.Equal(new DateOnly(2026, 5, 15), comparison.Recommended.PaidOffOn);

        var store = comparison.Recommended.Debts.Single(debt => debt.DebtId == StoreId);
        var card = comparison.Recommended.Debts.Single(debt => debt.DebtId == CardId);
        Assert.Equal(new DateOnly(2026, 1, 15), store.PaidOffOn);
        Assert.Equal(1, store.PaymentsUntilPaidOff);
        Assert.Equal(5, comparison.Avalanche.Debts.Single(debt => debt.DebtId == StoreId).PaymentsUntilPaidOff);
        Assert.Equal(new DateOnly(2026, 5, 15), card.PaidOffOn);
        Assert.Equal(0m, store.EndingBalance);

        var minimum = Adjustment(comparison, PayoffAdjustmentKind.MinimumRelease);
        Assert.True(minimum.Applied);
        Assert.Equal(3.89m, minimum.InterestDifference);
        Assert.Equal(100m, minimum.TradeoffCash);
        Assert.Contains("removes that minimum", minimum.Explanation, StringComparison.Ordinal);
        Assert.False(Adjustment(comparison, PayoffAdjustmentKind.Utilization).Applied);
        Assert.Contains("90 percent", Adjustment(comparison, PayoffAdjustmentKind.Utilization).Explanation, StringComparison.Ordinal);
        Assert.False(Adjustment(comparison, PayoffAdjustmentKind.Constraint).Applied);
        Assert.Contains("not rolled", comparison.Assumptions[2], StringComparison.Ordinal);
        Assert.Contains("100.00 USD", minimum.Explanation, StringComparison.Ordinal);
        Assert.Contains("3.89 USD", minimum.Explanation, StringComparison.Ordinal);
    }

    [Fact]
    public void Compare_KeepsAvalancheWhenRemovingAMinimumCostsMoreThanItKeeps()
    {
        var comparison = PayoffPriority.Compare(Input(
            300m,
            [
                Debt(StoreId, "Loan", 8000m, 4m, 150m, kind: DebtKind.Installment),
                Debt(CardId, "Card", 2000m, 28m, 80m, limit: 4000m)
            ]));

        Assert.Equal([CardId, StoreId], comparison.Avalanche.DebtIds);
        Assert.Equal(comparison.Avalanche.DebtIds, comparison.Recommended.DebtIds);
        var minimum = Adjustment(comparison, PayoffAdjustmentKind.MinimumRelease);
        Assert.False(minimum.Applied);
        Assert.Equal([StoreId, CardId], minimum.DebtIds);
        Assert.True(minimum.TradeoffCash <= minimum.InterestDifference);
        Assert.Contains("Avalanche stays", minimum.Explanation, StringComparison.Ordinal);
    }

    [Fact]
    public void Compare_LowersHighUtilizationWhenThatCashBeatsTheExtraInterest()
    {
        var comparison = PayoffPriority.Compare(Input(
            150m,
            [
                Debt(VisaId, "Visa", 1900m, 12m, 35m, limit: 2000m),
                Debt(CardId, "Card", 1900m, 24m, 40m, limit: 10000m)
            ],
            userOrder: [VisaId, CardId]));

        Assert.Equal([CardId, VisaId], comparison.Avalanche.DebtIds);
        Assert.Equal([VisaId, CardId], comparison.Recommended.DebtIds);
        Assert.Equal([VisaId, CardId], comparison.UserSelected.DebtIds);
        Assert.True(comparison.UserOrderProvided);

        var recommendedVisa = comparison.Recommended.Debts.Single(debt => debt.DebtId == VisaId);
        var avalancheVisa = comparison.Avalanche.Debts.Single(debt => debt.DebtId == VisaId);
        Assert.Equal(0.95m, recommendedVisa.Utilization);
        Assert.Equal(1, recommendedVisa.PaymentsUntilUnderLimit);
        Assert.True(avalancheVisa.PaymentsUntilUnderLimit > 1);
        var avalancheCard = comparison.Avalanche.Debts.Single(debt => debt.DebtId == CardId);
        var recommendedCard = comparison.Recommended.Debts.Single(debt => debt.DebtId == CardId);
        Assert.Equal(avalancheCard.PaymentsUntilPaidOff + 1, recommendedCard.PaymentsUntilPaidOff);
        Assert.True(comparison.UserSelected.TotalInterest > comparison.Recommended.TotalInterest);

        var utilization = Adjustment(comparison, PayoffAdjustmentKind.Utilization);
        Assert.True(utilization.Applied);
        Assert.False(Adjustment(comparison, PayoffAdjustmentKind.MinimumRelease).Applied);
        Assert.True(utilization.TradeoffCash > utilization.InterestDifference);
        Assert.True(comparison.Recommended.TotalInterest > comparison.Avalanche.TotalInterest);
        Assert.Contains("returns to avalanche", utilization.Explanation, StringComparison.Ordinal);
    }

    [Fact]
    public void Compare_PrefersTheLargerTradeoffWhenBothReasonsQualify()
    {
        var comparison = PayoffPriority.Compare(Input(
            150m,
            [
                Debt(StoreId, "Store", 100m, 10m, 25m, limit: 1000m),
                Debt(VisaId, "Visa", 1900m, 12m, 35m, limit: 2000m),
                Debt(CardId, "Card", 1900m, 24m, 40m, limit: 10000m)
            ],
            userOrder: [StoreId, CardId, VisaId]));

        Assert.Equal([CardId, VisaId, StoreId], comparison.Avalanche.DebtIds);
        Assert.Equal([VisaId, CardId, StoreId], comparison.Recommended.DebtIds);
        Assert.Equal([StoreId, CardId, VisaId], comparison.UserSelected.DebtIds);
        Assert.True(Adjustment(comparison, PayoffAdjustmentKind.Utilization).Applied);
        var minimum = Adjustment(comparison, PayoffAdjustmentKind.MinimumRelease);
        Assert.False(minimum.Applied);
        Assert.Equal(StoreId, minimum.DebtIds[0]);
        Assert.Contains("larger tradeoff", minimum.Explanation, StringComparison.Ordinal);
    }

    [Fact]
    public void Compare_HonorsAPayFirstConstraintAndStillReturnsAvalanche()
    {
        var comparison = PayoffPriority.Compare(Input(
            100m,
            [
                Debt(StoreId, "Store", 100m, 10m, 25m, limit: 1000m),
                Debt(CardId, "Card", 500m, 20m, 25m, limit: 2000m)
            ],
            userOrder: [CardId, StoreId],
            constraints: [new PayoffConstraint(StoreId, PayoffConstraintKind.PayFirst)]));

        Assert.Equal([CardId, StoreId], comparison.Avalanche.DebtIds);
        Assert.Equal([StoreId, CardId], comparison.Recommended.DebtIds);
        Assert.Equal([CardId, StoreId], comparison.UserSelected.DebtIds);
        var constraint = Adjustment(comparison, PayoffAdjustmentKind.Constraint);
        Assert.True(constraint.Applied);
        Assert.True(constraint.InterestDifference > 0);
        Assert.Contains("honors that constraint", constraint.Explanation, StringComparison.Ordinal);
        Assert.Contains("constraint instead", Adjustment(comparison, PayoffAdjustmentKind.MinimumRelease).Explanation, StringComparison.Ordinal);
        Assert.False(Adjustment(comparison, PayoffAdjustmentKind.MinimumRelease).Applied);
    }

    [Fact]
    public void Compare_HonorsPayLastByMovingThatDebtBehindTheOthers()
    {
        var comparison = PayoffPriority.Compare(Input(
            50m,
            [
                Debt(StoreId, "Store", 400m, 8m, 25m),
                Debt(CardId, "Card", 400m, 18m, 25m)
            ],
            constraints: [new PayoffConstraint(CardId, PayoffConstraintKind.PayLast)]));

        Assert.Equal([CardId, StoreId], comparison.Avalanche.DebtIds);
        Assert.Equal([StoreId, CardId], comparison.Recommended.DebtIds);
        Assert.True(Adjustment(comparison, PayoffAdjustmentKind.Constraint).Applied);
        Assert.Contains("Pay Card last", Adjustment(comparison, PayoffAdjustmentKind.Constraint).Explanation, StringComparison.Ordinal);
    }

    [Fact]
    public void Compare_LeavesOutAnotherCurrencyAndDoesNotPayAMissingRate()
    {
        var comparison = PayoffPriority.Compare(Input(
            100m,
            [
                Debt(VisaId, "Unknown", 80m, null, 10m),
                Debt(StoreId, "Store", 100m, 0m, 10m),
                Debt(CardId, "Canada", 500m, 20m, 25m, currency: "CAD")
            ]));

        Assert.Equal([StoreId, VisaId], comparison.Avalanche.DebtIds);
        Assert.Equal(["CAD"], comparison.ExcludedCurrencies);
        Assert.Contains("CAD", comparison.Assumptions[^1], StringComparison.Ordinal);
        var unknown = comparison.Avalanche.Debts.Single(debt => debt.DebtId == VisaId);
        var store = comparison.Avalanche.Debts.Single(debt => debt.DebtId == StoreId);
        Assert.Equal(DebtScheduleStop.RateUnknown, unknown.Stop);
        Assert.Equal(80m, unknown.EndingBalance);
        Assert.Equal(0m, unknown.Interest);
        Assert.Equal(1, store.PaymentsUntilPaidOff);
        Assert.Equal(0m, store.EndingBalance);
    }

    [Fact]
    public void Compare_RanksByTheRateOnTheFirstDueDateAndThenByTheSmallerBalance()
    {
        var promo = new DebtAmortizationInput(
            StoreId,
            "Promo",
            "USD",
            DebtKind.Revolving,
            1000m,
            30m,
            0m,
            new DateOnly(2026, 3, 15),
            25m,
            null,
            Due,
            0m);
        var comparison = PayoffPriority.Compare(Input(
            0m,
            [
                new PayoffDebt(promo, 2000m),
                Debt(CardId, "Card", 1000m, 20m, 25m),
                Debt(VisaId, "Small", 100m, 20m, 25m)
            ]));

        Assert.Equal([VisaId, CardId, StoreId], comparison.Avalanche.DebtIds);
        Assert.Equal(0m, comparison.Avalanche.Debts.Single(debt => debt.DebtId == StoreId).Apr);
    }

    [Fact]
    public void Compare_StepsAMonthEndDueDateFromTheOriginalDate()
    {
        var comparison = PayoffPriority.Compare(Input(
            0m,
            [Debt(StoreId, "Loan", 20m, 0m, 10m, due: new DateOnly(2026, 1, 31))]));

        var loan = comparison.Avalanche.Debts.Single();
        Assert.Equal(new DateOnly(2026, 2, 28), loan.PaidOffOn);
        Assert.Equal(2, loan.PaymentsUntilPaidOff);
        Assert.Equal(0m, loan.Interest);
    }

    [Fact]
    public void Compare_StopsWhenAPaymentDoesNotReduceTheBalance()
    {
        var comparison = PayoffPriority.Compare(Input(
            0m,
            [
                Debt(CardId, "Card", 1000m, 24m, 10m),
                Debt(StoreId, "Store", 50m, 0m, 50m)
            ]));

        var card = comparison.Avalanche.Debts.Single(debt => debt.DebtId == CardId);
        Assert.Equal(DebtScheduleStop.DoesNotPayDown, card.Stop);
        Assert.Equal(1010m, card.EndingBalance);
        Assert.Equal(20m, card.Interest);
        Assert.Equal(DebtScheduleStop.PaidOff, comparison.Avalanche.Debts.Single(debt => debt.DebtId == StoreId).Stop);
        Assert.Null(comparison.Avalanche.PaidOffOn);
    }

    [Fact]
    public void Compare_KeepsOwnExtraOnItsDebtWhenTheOrderChanges()
    {
        var comparison = PayoffPriority.Compare(Input(
            0m,
            [
                Debt(StoreId, "Store", 200m, 0m, 50m, extra: 50m),
                Debt(CardId, "Card", 200m, 0m, 50m)
            ],
            userOrder: [CardId, StoreId]));

        Assert.Equal(comparison.Avalanche.Debts.Single(debt => debt.DebtId == StoreId).PaymentsUntilPaidOff, comparison.UserSelected.Debts.Single(debt => debt.DebtId == StoreId).PaymentsUntilPaidOff);
        Assert.Equal(2, comparison.UserSelected.Debts.Single(debt => debt.DebtId == StoreId).PaymentsUntilPaidOff);
        Assert.Equal(4, comparison.UserSelected.Debts.Single(debt => debt.DebtId == CardId).PaymentsUntilPaidOff);
        Assert.Equal(0m, comparison.Avalanche.TotalInterest);
        Assert.Equal(0m, comparison.UserSelected.TotalInterest);
    }

    [Fact]
    public void Compare_SkipsAConstraintThatCannotBePaid()
    {
        var comparison = PayoffPriority.Compare(Input(
            20m,
            [Debt(StoreId, "Store", 100m, null, 25m)],
            constraints: [new PayoffConstraint(StoreId, PayoffConstraintKind.PayFirst)]));

        Assert.False(Adjustment(comparison, PayoffAdjustmentKind.Constraint).Applied);
        Assert.Contains("cannot take a payment", Adjustment(comparison, PayoffAdjustmentKind.Constraint).Explanation, StringComparison.Ordinal);
        Assert.Equal(DebtScheduleStop.RateUnknown, comparison.Avalanche.Debts.Single().Stop);
    }

    [Fact]
    public void Compare_RepeatsTheSameOrdersAndCents()
    {
        var input = Input(
            100m,
            [
                Debt(StoreId, "Store", 100m, 10m, 25m, limit: 1000m),
                Debt(CardId, "Card", 500m, 20m, 25m, limit: 2000m)
            ],
            userOrder: [StoreId, CardId]);
        var first = PayoffPriority.Compare(input);
        var again = PayoffPriority.Compare(input);

        Assert.Equal(first.Avalanche.DebtIds, again.Avalanche.DebtIds);
        Assert.Equal(first.Recommended.DebtIds, again.Recommended.DebtIds);
        Assert.Equal(first.UserSelected.DebtIds, again.UserSelected.DebtIds);
        Assert.Equal(first.Avalanche.TotalInterest, again.Avalanche.TotalInterest);
        Assert.Equal(first.Recommended.TotalInterest, again.Recommended.TotalInterest);
        Assert.Equal(first.UserSelected.TotalInterest, again.UserSelected.TotalInterest);
        Assert.Equal(
            first.Adjustments.Select(item => (item.Kind, item.Applied, item.InterestDifference, item.TradeoffCash, item.Explanation)),
            again.Adjustments.Select(item => (item.Kind, item.Applied, item.InterestDifference, item.TradeoffCash, item.Explanation)));
        Assert.Equal(first.Assumptions, again.Assumptions);
    }

    [Fact]
    public void Compare_TreatsANegativeExtraAsNothingToDirect()
    {
        var comparison = PayoffPriority.Compare(Input(
            -25m,
            [
                Debt(StoreId, "Store", 100m, 10m, 25m),
                Debt(CardId, "Card", 100m, 20m, 25m)
            ]));

        Assert.Equal(0m, comparison.MonthlyExtra);
        Assert.Equal(comparison.Avalanche.TotalInterest, comparison.Recommended.TotalInterest);
        Assert.Equal(comparison.Avalanche.DebtIds, comparison.Recommended.DebtIds);
    }

    [Fact]
    public void Compare_KeepsTheFirstCopyOfARepeatedDebt()
    {
        var comparison = PayoffPriority.Compare(Input(
            10m,
            [
                Debt(StoreId, "Store", 80m, 9m, 20m),
                Debt(StoreId, "Copy", 500m, 30m, 40m),
                Debt(CardId, "Card", 80m, 4m, 20m)
            ]));

        Assert.Equal([StoreId, CardId], comparison.Avalanche.DebtIds);
        Assert.Equal("Store", comparison.Avalanche.Debts[0].Name);
        Assert.Equal(9m, comparison.Avalanche.Debts[0].Apr);
    }

    [Fact]
    public void Compare_LeavesUtilizationAloneWhenThatCardIsAlreadyFirst()
    {
        var comparison = PayoffPriority.Compare(Input(
            150m,
            [
                Debt(VisaId, "Visa", 1900m, 24m, 35m, limit: 2000m),
                Debt(CardId, "Card", 1900m, 12m, 40m, limit: 10000m)
            ]));

        Assert.Equal([VisaId, CardId], comparison.Avalanche.DebtIds);
        Assert.Equal(comparison.Avalanche.DebtIds, comparison.Recommended.DebtIds);
        var utilization = Adjustment(comparison, PayoffAdjustmentKind.Utilization);
        Assert.False(utilization.Applied);
        Assert.Contains("already first", utilization.Explanation, StringComparison.Ordinal);
    }

    private static PayoffAdjustment Adjustment(PayoffComparison comparison, PayoffAdjustmentKind kind)
    {
        return comparison.Adjustments.Single(item => item.Kind == kind);
    }

    private static PayoffPriorityInput Input(
        decimal extra,
        IReadOnlyList<PayoffDebt> debts,
        IReadOnlyList<Guid>? userOrder = null,
        IReadOnlyList<PayoffConstraint>? constraints = null)
    {
        return new PayoffPriorityInput(
            "USD",
            extra,
            debts,
            userOrder ?? [],
            constraints ?? []);
    }

    private static PayoffDebt Debt(
        Guid id,
        string name,
        decimal balance,
        decimal? apr,
        decimal? minimum,
        decimal extra = 0m,
        decimal? limit = null,
        DebtKind kind = DebtKind.Revolving,
        string currency = "USD",
        DateOnly? due = null)
    {
        return new PayoffDebt(
            new DebtAmortizationInput(
                id,
                name,
                currency,
                kind,
                balance,
                apr,
                null,
                null,
                minimum,
                null,
                due ?? Due,
                extra),
            limit);
    }
}
