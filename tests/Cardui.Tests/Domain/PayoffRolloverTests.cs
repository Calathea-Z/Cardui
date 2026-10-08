using Cardui.Api.Domain.Recovery;
using Cardui.Api.Models;
using Xunit;

namespace Cardui.Tests.Domain;

public class PayoffRolloverTests
{
    private static readonly Guid StoreId = Guid.Parse("40000000-0000-0000-0000-000000000001");
    private static readonly Guid CardId = Guid.Parse("40000000-0000-0000-0000-000000000002");
    private static readonly Guid VisaId = Guid.Parse("40000000-0000-0000-0000-000000000003");
    private static readonly DateOnly Due = new(2026, 1, 15);

    [Fact]
    public void Compare_RollsTheMinimumAndPlannedExtraTheMonthAfterThePaymentEnds()
    {
        var comparison = PayoffRollover.Compare(Input(
            0m,
            [
                Debt(StoreId, "Store", 100m, 0m, 50m, extra: 10m),
                Debt(CardId, "Card", 200m, 0m, 25m)
            ]));

        var store = comparison.Rollover.Debts.Single(debt => debt.DebtId == StoreId);
        var card = comparison.Rollover.Debts.Single(debt => debt.DebtId == CardId);
        Assert.Equal(new DateOnly(2026, 2, 15), store.PaidOffOn);
        Assert.Equal(2, store.PaymentsUntilPaidOff);
        Assert.Equal(new DateOnly(2026, 4, 15), card.PaidOffOn);
        Assert.Equal(4, card.PaymentsUntilPaidOff);
        Assert.Equal(new DateOnly(2026, 4, 15), comparison.Rollover.PaidOffOn);
        Assert.Equal(100m, comparison.Rollover.CashRolled);
        Assert.Equal(0m, comparison.Rollover.CashReclaimed);
        Assert.Equal(0m, comparison.Rollover.InterestDifference);

        var freed = comparison.Rollover.FreedPayments.Single(payment => payment.DebtId == StoreId);
        Assert.Equal(50m, freed.Minimum);
        Assert.Equal(10m, freed.Extra);
        Assert.Equal(60m, freed.Amount);
        Assert.Equal(new DateOnly(2026, 2, 15), freed.EndedOn);
        Assert.Contains("following month", comparison.Rollover.Explanation, StringComparison.Ordinal);
        Assert.Contains("50.00 USD", comparison.Rollover.Explanation, StringComparison.Ordinal);
        Assert.Contains("10.00 USD", comparison.Rollover.Explanation, StringComparison.Ordinal);
        Assert.Contains("April 15, 2026", comparison.Rollover.Explanation, StringComparison.Ordinal);

        var held = comparison.ReclaimAll.Debts.Single(debt => debt.DebtId == CardId);
        Assert.Equal(new DateOnly(2026, 8, 15), held.PaidOffOn);
        Assert.Equal(8, held.PaymentsUntilPaidOff);
        Assert.Equal(360m, comparison.ReclaimAll.CashReclaimed);
        Assert.Equal(0m, comparison.ReclaimAll.CashRolled);
        Assert.Equal(0m, comparison.ReclaimAll.InterestDifference);
        Assert.Contains("savings or spending", comparison.ReclaimAll.Explanation, StringComparison.Ordinal);
        Assert.Contains("following month", comparison.Assumptions[1], StringComparison.Ordinal);
    }

    [Fact]
    public void Compare_CostsMoreInterestToReclaimSomeAndMoreToReclaimAll()
    {
        var comparison = PayoffRollover.Compare(Input(
            0m,
            [
                Debt(StoreId, "Store", 50m, 0m, 50m),
                Debt(CardId, "Card", 100m, 12m, 25m)
            ],
            reclaim: 20m));

        Assert.Equal([CardId, StoreId], comparison.Rollover.DebtIds);
        Assert.False(comparison.OrderProvided);
        Assert.Equal(new DateOnly(2026, 1, 15), comparison.Rollover.Debts.Single(debt => debt.DebtId == StoreId).PaidOffOn);
        Assert.Equal(3, comparison.Rollover.Debts.Single(debt => debt.DebtId == CardId).PaymentsUntilPaidOff);
        Assert.Equal(new DateOnly(2026, 3, 15), comparison.Rollover.PaidOffOn);
        Assert.Equal(1.78m, comparison.Rollover.TotalInterest);
        Assert.Equal(50m, comparison.Rollover.CashRolled);

        Assert.Equal(20m, comparison.ReclaimAmount);
        Assert.Equal(1.98m, comparison.Reclaim.TotalInterest);
        Assert.Equal(0.20m, comparison.Reclaim.InterestDifference);
        Assert.Equal(40m, comparison.Reclaim.CashReclaimed);
        Assert.Equal(30m, comparison.Reclaim.CashRolled);
        Assert.Equal(new DateOnly(2026, 3, 15), comparison.Reclaim.PaidOffOn);
        Assert.Contains("20.00 USD", comparison.Reclaim.Explanation, StringComparison.Ordinal);
        Assert.Contains("40.00 USD", comparison.Reclaim.Explanation, StringComparison.Ordinal);
        Assert.Contains("0.20 USD higher", comparison.Reclaim.Explanation, StringComparison.Ordinal);

        Assert.Equal(2.58m, comparison.ReclaimAll.TotalInterest);
        Assert.Equal(0.80m, comparison.ReclaimAll.InterestDifference);
        Assert.Equal(200m, comparison.ReclaimAll.CashReclaimed);
        Assert.Equal(0m, comparison.ReclaimAll.CashRolled);
        Assert.Equal(5, comparison.ReclaimAll.Debts.Single(debt => debt.DebtId == CardId).PaymentsUntilPaidOff);
        Assert.Equal(new DateOnly(2026, 5, 15), comparison.ReclaimAll.PaidOffOn);
        Assert.Contains("200.00 USD", comparison.ReclaimAll.Explanation, StringComparison.Ordinal);
        Assert.Contains("0.80 USD higher", comparison.ReclaimAll.Explanation, StringComparison.Ordinal);
    }

    [Fact]
    public void Compare_SendsRolledCashToTheFirstOpenDebtInTheGivenOrder()
    {
        var forward = PayoffRollover.Compare(Input(
            0m,
            [
                Debt(VisaId, "Tiny", 30m, 0m, 30m),
                Debt(StoreId, "Alpha", 100m, 0m, 10m),
                Debt(CardId, "Beta", 100m, 0m, 10m)
            ],
            order: [VisaId, StoreId, CardId]));
        var reverse = PayoffRollover.Compare(Input(
            0m,
            [
                Debt(VisaId, "Tiny", 30m, 0m, 30m),
                Debt(StoreId, "Alpha", 100m, 0m, 10m),
                Debt(CardId, "Beta", 100m, 0m, 10m)
            ],
            order: [VisaId, CardId, StoreId]));

        Assert.True(forward.OrderProvided);
        Assert.Equal([VisaId, StoreId, CardId], forward.Rollover.DebtIds);
        Assert.Equal(4, forward.Rollover.Debts.Single(debt => debt.DebtId == StoreId).PaymentsUntilPaidOff);
        Assert.Equal(5, forward.Rollover.Debts.Single(debt => debt.DebtId == CardId).PaymentsUntilPaidOff);
        Assert.Equal(new DateOnly(2026, 4, 15), forward.Rollover.Debts.Single(debt => debt.DebtId == StoreId).PaidOffOn);
        Assert.Equal(new DateOnly(2026, 5, 15), forward.Rollover.Debts.Single(debt => debt.DebtId == CardId).PaidOffOn);
        Assert.Equal(110m, forward.Rollover.CashRolled);

        Assert.Equal([VisaId, CardId, StoreId], reverse.Rollover.DebtIds);
        Assert.Equal(4, reverse.Rollover.Debts.Single(debt => debt.DebtId == CardId).PaymentsUntilPaidOff);
        Assert.Equal(5, reverse.Rollover.Debts.Single(debt => debt.DebtId == StoreId).PaymentsUntilPaidOff);
    }

    [Fact]
    public void Compare_DoesNotTreatSharedExtraAsAFreedPayment()
    {
        var comparison = PayoffRollover.Compare(Input(
            75m,
            [
                Debt(StoreId, "Store", 100m, 0m, 25m),
                Debt(CardId, "Card", 200m, 0m, 25m)
            ]));

        Assert.Equal(new DateOnly(2026, 3, 15), comparison.Rollover.PaidOffOn);
        Assert.Equal(25m, comparison.Rollover.CashRolled);
        var freed = comparison.Rollover.FreedPayments.Single(payment => payment.DebtId == StoreId);
        Assert.Equal(25m, freed.Minimum);
        Assert.Equal(0m, freed.Extra);
        Assert.Equal(25m, freed.Amount);
        Assert.Contains("not part of the freed payment", comparison.Assumptions[3], StringComparison.Ordinal);
    }

    [Fact]
    public void Compare_FreesThePlannedMinimumWhenTheLastPaymentIsSmaller()
    {
        var comparison = PayoffRollover.Compare(Input(
            0m,
            [
                Debt(StoreId, "Store", 30m, 0m, 50m, extra: 10m),
                Debt(CardId, "Card", 100m, 0m, 10m)
            ]));

        var freed = comparison.Rollover.FreedPayments.Single(payment => payment.DebtId == StoreId);
        Assert.Equal(new DateOnly(2026, 1, 15), freed.EndedOn);
        Assert.Equal(60m, freed.Amount);
        Assert.Equal(new DateOnly(2026, 3, 15), comparison.Rollover.Debts.Single(debt => debt.DebtId == CardId).PaidOffOn);
        Assert.Equal(70m, comparison.Rollover.CashRolled);
        Assert.Contains("smaller because the balance was smaller", comparison.Assumptions[2], StringComparison.Ordinal);
    }

    [Fact]
    public void Compare_DoesNotFreeAPaymentThatFailsToReduceTheBalance()
    {
        var comparison = PayoffRollover.Compare(Input(
            0m,
            [
                Debt(CardId, "Card", 1000m, 24m, 10m),
                Debt(StoreId, "Store", 50m, 0m, 50m)
            ]));

        var card = comparison.Rollover.Debts.Single(debt => debt.DebtId == CardId);
        Assert.Equal(DebtScheduleStop.DoesNotPayDown, card.Stop);
        Assert.Equal(1010m, card.EndingBalance);
        Assert.Equal(DebtScheduleStop.PaidOff, comparison.Rollover.Debts.Single(debt => debt.DebtId == StoreId).Stop);
        Assert.Equal(0m, comparison.Rollover.CashRolled);
        Assert.Equal(0m, comparison.Rollover.CashReclaimed);
        Assert.Equal(0m, comparison.ReclaimAll.CashReclaimed);
        Assert.Null(comparison.Rollover.PaidOffOn);
        Assert.Contains("No freed payment rolls", comparison.Rollover.Explanation, StringComparison.Ordinal);
        Assert.Contains("still open", comparison.Rollover.Explanation, StringComparison.Ordinal);
        Assert.Contains("no freed cash to reclaim", comparison.ReclaimAll.Explanation, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Compare_WaitsUntilTheNextRoundAfterAMonthEndPayoff()
    {
        var due = new DateOnly(2026, 1, 31);
        var comparison = PayoffRollover.Compare(Input(
            0m,
            [
                Debt(StoreId, "Store", 10m, 0m, 10m, due: due),
                Debt(CardId, "Card", 40m, 0m, 10m, due: due)
            ]));

        Assert.Equal(new DateOnly(2026, 1, 31), comparison.Rollover.Debts.Single(debt => debt.DebtId == StoreId).PaidOffOn);
        Assert.Equal(new DateOnly(2026, 3, 31), comparison.Rollover.Debts.Single(debt => debt.DebtId == CardId).PaidOffOn);
        Assert.Equal(3, comparison.Rollover.Debts.Single(debt => debt.DebtId == CardId).PaymentsUntilPaidOff);
        Assert.Equal(10m, comparison.Rollover.CashRolled);
    }

    [Fact]
    public void Compare_LeavesOutAnotherCurrencyAndDoesNotRollOntoAMissingRate()
    {
        var comparison = PayoffRollover.Compare(Input(
            0m,
            [
                Debt(StoreId, "Store", 50m, 0m, 50m),
                Debt(CardId, "Unknown", 80m, null, 10m),
                Debt(VisaId, "Canada", 500m, 20m, 25m, currency: "CAD")
            ]));

        Assert.Equal(["CAD"], comparison.ExcludedCurrencies);
        Assert.Contains("CAD", comparison.Assumptions[^1], StringComparison.Ordinal);
        Assert.Equal([StoreId, CardId], comparison.Rollover.DebtIds);
        Assert.Equal(DebtScheduleStop.PaidOff, comparison.Rollover.Debts.Single(debt => debt.DebtId == StoreId).Stop);
        Assert.Equal(DebtScheduleStop.RateUnknown, comparison.Rollover.Debts.Single(debt => debt.DebtId == CardId).Stop);
        Assert.Equal(80m, comparison.Rollover.Debts.Single(debt => debt.DebtId == CardId).EndingBalance);
        Assert.Equal(0m, comparison.Rollover.CashRolled);
        Assert.Null(comparison.Rollover.PaidOffOn);
    }

    [Fact]
    public void Compare_MatchesRolloverWhenNothingIsReclaimedAndMatchesReclaimAllWhenTheAmountCoversIt()
    {
        var debts = new[]
        {
            Debt(StoreId, "Store", 50m, 0m, 50m),
            Debt(CardId, "Card", 100m, 12m, 25m)
        };
        var nothing = PayoffRollover.Compare(Input(0m, debts));
        var covered = PayoffRollover.Compare(Input(0m, debts, reclaim: 10_000m));

        Assert.Equal(0m, nothing.ReclaimAmount);
        Assert.Equal(nothing.Rollover.TotalInterest, nothing.Reclaim.TotalInterest);
        Assert.Equal(nothing.Rollover.PaidOffOn, nothing.Reclaim.PaidOffOn);
        Assert.Equal(0m, nothing.Reclaim.CashReclaimed);
        Assert.Equal(nothing.Rollover.CashRolled, nothing.Reclaim.CashRolled);
        Assert.Contains("Reclaiming nothing", nothing.Reclaim.Explanation, StringComparison.Ordinal);

        Assert.Equal(covered.ReclaimAll.TotalInterest, covered.Reclaim.TotalInterest);
        Assert.Equal(covered.ReclaimAll.PaidOffOn, covered.Reclaim.PaidOffOn);
        Assert.Equal(covered.ReclaimAll.CashReclaimed, covered.Reclaim.CashReclaimed);
        Assert.Equal(0m, covered.Reclaim.CashRolled);
        Assert.Contains("all of the freed cash", covered.Reclaim.Explanation, StringComparison.Ordinal);
    }

    [Fact]
    public void Compare_TreatsANegativeExtraAndANegativeReclaimAsNothing()
    {
        var comparison = PayoffRollover.Compare(Input(
            -25m,
            [
                Debt(StoreId, "Store", 50m, 0m, 50m),
                Debt(CardId, "Card", 100m, 12m, 25m)
            ],
            reclaim: -10m));

        Assert.Equal(0m, comparison.MonthlyExtra);
        Assert.Equal(0m, comparison.ReclaimAmount);
        Assert.Equal(comparison.Rollover.TotalInterest, comparison.Reclaim.TotalInterest);
        Assert.Equal(1.78m, comparison.Rollover.TotalInterest);
    }

    [Fact]
    public void Compare_KeepsTheFirstCopyAndDropsAZeroBalance()
    {
        var comparison = PayoffRollover.Compare(Input(
            0m,
            [
                Debt(StoreId, "Store", 50m, 0m, 50m),
                Debt(StoreId, "Copy", 500m, 30m, 40m),
                Debt(CardId, "Card", 0m, 20m, 25m)
            ]));

        Assert.Equal([StoreId], comparison.Rollover.DebtIds);
        Assert.Equal("Store", comparison.Rollover.Debts[0].Name);
        Assert.Contains("No freed payment rolls", comparison.Rollover.Explanation, StringComparison.Ordinal);
    }

    [Fact]
    public void Compare_RecordsEachBalanceAfterEveryPaymentAndNoneForADebtItCannotCalculate()
    {
        var comparison = PayoffRollover.Compare(Input(
            0m,
            [
                Debt(StoreId, "Store", 100m, 0m, 50m, extra: 10m),
                Debt(CardId, "Card", 200m, 0m, 25m),
                Debt(VisaId, "Visa", 50m, 12m, null)
            ]));

        var rollover = comparison.Rollover;
        Assert.Equal(
            [(new DateOnly(2026, 1, 15), 40m), (new DateOnly(2026, 2, 15), 0m)],
            PointsFor(rollover, StoreId));
        Assert.Equal(
            [
                (new DateOnly(2026, 1, 15), 175m),
                (new DateOnly(2026, 2, 15), 150m),
                (new DateOnly(2026, 3, 15), 65m),
                (new DateOnly(2026, 4, 15), 0m)
            ],
            PointsFor(rollover, CardId));
        Assert.Empty(PointsFor(rollover, VisaId));

        var order = rollover.DebtIds.ToList();
        var keys = rollover.BalancePoints
            .Select(point => (point.DueDate, order.IndexOf(point.DebtId)))
            .ToList();
        Assert.Equal(keys.OrderBy(key => key.DueDate).ThenBy(key => key.Item2), keys);

        var kept = PointsFor(comparison.ReclaimAll, CardId);
        Assert.Equal(8, kept.Count);
        Assert.Equal((new DateOnly(2026, 8, 15), 0m), kept[^1]);
    }

    [Fact]
    public void Compare_RecordsTheInterestAndPaymentOfTheMonthThatDoesNotPayDown()
    {
        var comparison = PayoffRollover.Compare(Input(
            0m,
            [Debt(CardId, "Card", 1000m, 24m, 15m)]));

        var card = Assert.Single(comparison.Rollover.Debts);
        Assert.Equal(DebtScheduleStop.DoesNotPayDown, card.Stop);
        var point = Assert.Single(comparison.Rollover.BalancePoints);
        Assert.Equal(Due, point.DueDate);
        Assert.Equal(1005m, point.Balance);
        Assert.Equal(20m, point.Interest);
        Assert.Equal(15m, point.Payment);
    }

    [Fact]
    public void Compare_RepeatsTheSamePaymentsAndCents()
    {
        var input = Input(
            0m,
            [
                Debt(StoreId, "Store", 100m, 0m, 50m, extra: 10m),
                Debt(CardId, "Card", 200m, 0m, 25m)
            ],
            reclaim: 20m,
            order: [StoreId, CardId]);
        var first = PayoffRollover.Compare(input);
        var again = PayoffRollover.Compare(input);

        Assert.Equal(first.Rollover.DebtIds, again.Rollover.DebtIds);
        Assert.Equal(first.Rollover.TotalInterest, again.Rollover.TotalInterest);
        Assert.Equal(first.Rollover.PaidOffOn, again.Rollover.PaidOffOn);
        Assert.Equal(first.Rollover.CashRolled, again.Rollover.CashRolled);
        Assert.Equal(first.Reclaim.CashReclaimed, again.Reclaim.CashReclaimed);
        Assert.Equal(first.Reclaim.InterestDifference, again.Reclaim.InterestDifference);
        Assert.Equal(first.ReclaimAll.TotalInterest, again.ReclaimAll.TotalInterest);
        Assert.Equal(first.ReclaimAll.CashReclaimed, again.ReclaimAll.CashReclaimed);
        Assert.Equal(first.Rollover.Explanation, again.Rollover.Explanation);
        Assert.Equal(first.Reclaim.Explanation, again.Reclaim.Explanation);
        Assert.Equal(first.ReclaimAll.Explanation, again.ReclaimAll.Explanation);
        Assert.Equal(first.Assumptions, again.Assumptions);
    }

    private static List<(DateOnly DueDate, decimal Balance)> PointsFor(PayoffRolloverPath path, Guid debtId)
    {
        return path.BalancePoints
            .Where(point => point.DebtId == debtId)
            .Select(point => (point.DueDate, point.Balance))
            .ToList();
    }

    private static PayoffRolloverInput Input(
        decimal extra,
        IReadOnlyList<PayoffDebt> debts,
        IReadOnlyList<Guid>? order = null,
        decimal reclaim = 0m)
    {
        return new PayoffRolloverInput(
            "USD",
            extra,
            debts,
            order ?? [],
            reclaim);
    }

    private static PayoffDebt Debt(
        Guid id,
        string name,
        decimal balance,
        decimal? apr,
        decimal? minimum,
        decimal extra = 0m,
        string currency = "USD",
        DateOnly? due = null)
    {
        return new PayoffDebt(
            new DebtAmortizationInput(
                id,
                name,
                currency,
                DebtKind.Revolving,
                balance,
                apr,
                null,
                null,
                minimum,
                null,
                due ?? Due,
                extra),
            null);
    }
}
