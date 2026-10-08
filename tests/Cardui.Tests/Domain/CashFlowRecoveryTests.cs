using Cardui.Api.Domain.Recovery;
using Cardui.Api.Models;
using Xunit;

namespace Cardui.Tests.Domain;

public class CashFlowRecoveryTests
{
    private static readonly Guid StoreId = Guid.Parse("50000000-0000-0000-0000-000000000001");
    private static readonly Guid CardId = Guid.Parse("50000000-0000-0000-0000-000000000002");
    private static readonly Guid VisaId = Guid.Parse("50000000-0000-0000-0000-000000000003");
    private static readonly DateOnly Due = new(2026, 1, 15);

    [Fact]
    public void Track_RemovesEachMinimumTheMonthAfterThePayoffAndReleasesRoomWhenTheLastDebtStops()
    {
        var comparison = PayoffRollover.Compare(Input(
            0m,
            [
                Debt(StoreId, "Store", 100m, 0m, 50m, extra: 10m),
                Debt(CardId, "Card", 200m, 0m, 25m)
            ]));
        var recovery = CashFlowRecovery.Track(comparison);

        var storePayment = comparison.Rollover.FreedPayments.Single(payment => payment.DebtId == StoreId);
        Assert.Equal(new DateOnly(2026, 3, 15), storePayment.StartsOn);

        var rollover = recovery.Rollover;
        Assert.Equal(75m, rollover.StartingObligation);
        Assert.Equal(0m, rollover.RemainingObligation);
        Assert.Equal(0, rollover.UnknownRemaining);
        Assert.Equal(0m, rollover.ReleasedExtra);

        var store = rollover.Steps.Single(step => step.DebtId == StoreId);
        Assert.Equal(new DateOnly(2026, 2, 15), store.EndedOn);
        Assert.Equal(new DateOnly(2026, 3, 15), store.StartsOn);
        Assert.Equal(50m, store.Minimum);
        Assert.Equal(10m, store.Extra);
        Assert.Equal(60m, store.Amount);
        Assert.Equal(0m, store.BreathingRoomAdded);
        Assert.Equal(0m, store.BreathingRoom);

        var card = rollover.Steps.Single(step => step.DebtId == CardId);
        Assert.Equal(new DateOnly(2026, 4, 15), card.EndedOn);
        Assert.Equal(new DateOnly(2026, 5, 15), card.StartsOn);
        Assert.Equal(25m, card.Minimum);
        Assert.Equal(85m, card.BreathingRoomAdded);
        Assert.Equal(85m, card.BreathingRoom);
        Assert.Equal(85m, rollover.BreathingRoom);
        Assert.Equal(85m, rollover.RecurringRoom);
        Assert.Contains("removed on March 15, 2026", rollover.Explanation, StringComparison.Ordinal);
        Assert.Contains("stays committed to a later debt", rollover.Explanation, StringComparison.Ordinal);
        Assert.Contains("85.00 USD a month from May 15, 2026", rollover.Explanation, StringComparison.Ordinal);
        Assert.Contains("Recurring breathing room is 85.00 USD a month", rollover.Explanation, StringComparison.Ordinal);

        var kept = recovery.ReclaimAll;
        Assert.Equal(60m, kept.Steps.Single(step => step.DebtId == StoreId).BreathingRoom);
        Assert.Equal(new DateOnly(2026, 9, 15), kept.Steps.Single(step => step.DebtId == CardId).StartsOn);
        Assert.Equal(85m, kept.RecurringRoom);
        Assert.Contains("60.00 USD a month from March 15, 2026", kept.Explanation, StringComparison.Ordinal);
        Assert.Contains("next monthly due date", recovery.Assumptions[0], StringComparison.Ordinal);
    }

    [Fact]
    public void Track_KeepsTheReclaimAmountWhileADebtIsOpenAndReleasesTheRestAtTheEnd()
    {
        var recovery = CashFlowRecovery.Track(PayoffRollover.Compare(Input(
            0m,
            [
                Debt(StoreId, "Store", 50m, 0m, 50m),
                Debt(CardId, "Card", 100m, 12m, 25m)
            ],
            reclaim: 20m)));

        var partial = recovery.Reclaim;
        var store = partial.Steps.Single(step => step.DebtId == StoreId);
        Assert.Equal(new DateOnly(2026, 2, 15), store.StartsOn);
        Assert.Equal(20m, store.BreathingRoomAdded);
        Assert.Equal(20m, store.BreathingRoom);
        Assert.Contains("rest of the freed payment stays committed", partial.Explanation, StringComparison.Ordinal);

        var card = partial.Steps.Single(step => step.DebtId == CardId);
        Assert.Equal(new DateOnly(2026, 4, 15), card.StartsOn);
        Assert.Equal(55m, card.BreathingRoomAdded);
        Assert.Equal(75m, card.BreathingRoom);
        Assert.Equal(75m, partial.RecurringRoom);

        var all = recovery.ReclaimAll;
        Assert.Equal(50m, all.Steps.Single(step => step.DebtId == StoreId).BreathingRoom);
        Assert.Equal(new DateOnly(2026, 6, 15), all.Steps.Single(step => step.DebtId == CardId).StartsOn);
        Assert.Equal(75m, all.RecurringRoom);
        Assert.Equal(0m, recovery.Rollover.Steps.Single(step => step.DebtId == StoreId).BreathingRoom);
        Assert.Equal(75m, recovery.Rollover.RecurringRoom);
    }

    [Fact]
    public void Track_ReleasesSharedExtraOnlyWhenEveryDebtIsPaidOff()
    {
        var recovery = CashFlowRecovery.Track(PayoffRollover.Compare(Input(
            75m,
            [
                Debt(StoreId, "Store", 100m, 0m, 25m),
                Debt(CardId, "Card", 200m, 0m, 25m)
            ])));

        var rollover = recovery.Rollover;
        Assert.Equal(50m, rollover.StartingObligation);
        Assert.Equal(0m, rollover.Steps.Single(step => step.DebtId == StoreId).BreathingRoom);
        Assert.Equal(25m, rollover.Steps.Single(step => step.DebtId == StoreId).Amount);
        Assert.Equal(new DateOnly(2026, 4, 15), rollover.Steps.Single(step => step.DebtId == CardId).StartsOn);
        Assert.Equal(50m, rollover.BreathingRoom);
        Assert.Equal(75m, rollover.ReleasedExtra);
        Assert.Equal(125m, rollover.RecurringRoom);
        Assert.Contains("75.00 USD of extra is no longer sent", rollover.Explanation, StringComparison.Ordinal);
        Assert.Contains("not part of the freed payment", recovery.Assumptions[1], StringComparison.Ordinal);
        Assert.Equal(125m, recovery.ReclaimAll.RecurringRoom);
    }

    [Fact]
    public void Track_ReleasesAFreedMinimumThatHasNoDebtLeftToTakeIt()
    {
        var recovery = CashFlowRecovery.Track(PayoffRollover.Compare(Input(
            0m,
            [
                Debt(CardId, "Card", 1000m, 24m, 10m),
                Debt(StoreId, "Store", 50m, 0m, 50m)
            ],
            reclaim: 20m)));

        Assert.Equal(60m, recovery.Rollover.StartingObligation);
        Assert.Equal(10m, recovery.Rollover.RemainingObligation);
        Assert.Equal(50m, recovery.Rollover.RecurringRoom);
        Assert.Equal(0m, recovery.Rollover.ReleasedExtra);
        Assert.Equal(50m, recovery.Reclaim.RecurringRoom);
        Assert.Equal(50m, recovery.ReclaimAll.RecurringRoom);
        Assert.Contains("Card's 10.00 USD minimum remains", recovery.Rollover.Explanation, StringComparison.Ordinal);
        Assert.Contains("50.00 USD a month from February 15, 2026", recovery.Rollover.Explanation, StringComparison.Ordinal);
    }

    [Fact]
    public void Track_KeepsRolledCashCommittedWhenADebtReachesTheMonthCap()
    {
        var comparison = PayoffRollover.Compare(Input(
            0m,
            [
                Debt(StoreId, "Store", 20m, 0m, 20m),
                Debt(CardId, "Loan", 100_000m, 0m, 10m)
            ]));
        var recovery = CashFlowRecovery.Track(comparison);

        var rollover = recovery.Rollover;
        Assert.Equal(
            DebtScheduleStop.HorizonReached,
            comparison.Rollover.Debts.Single(debt => debt.DebtId == CardId).Stop);
        Assert.Equal(30m, rollover.StartingObligation);
        Assert.Equal(10m, rollover.RemainingObligation);
        Assert.Equal(0m, rollover.Steps.Single().BreathingRoomAdded);
        Assert.Equal(0m, rollover.RecurringRoom);
        Assert.Equal(0m, rollover.ReleasedExtra);
        Assert.Contains("Loan's 10.00 USD minimum remains", rollover.Explanation, StringComparison.Ordinal);
        Assert.Contains("stays committed to a later debt", rollover.Explanation, StringComparison.Ordinal);
        Assert.Contains("month cap", recovery.Assumptions[3], StringComparison.Ordinal);
    }

    [Fact]
    public void Track_UsesTheUnclippedDueDateAfterAMonthEndPayoff()
    {
        var due = new DateOnly(2026, 1, 31);
        var recovery = CashFlowRecovery.Track(PayoffRollover.Compare(Input(
            0m,
            [
                Debt(StoreId, "Store", 10m, 0m, 10m, due: due),
                Debt(CardId, "Card", 40m, 0m, 10m, due: due)
            ])));

        var rollover = recovery.Rollover;
        Assert.Equal(new DateOnly(2026, 2, 28), rollover.Steps.Single(step => step.DebtId == StoreId).StartsOn);
        Assert.Equal(0m, rollover.Steps.Single(step => step.DebtId == StoreId).BreathingRoom);
        Assert.Equal(new DateOnly(2026, 3, 31), rollover.Steps.Single(step => step.DebtId == CardId).EndedOn);
        Assert.Equal(new DateOnly(2026, 4, 30), rollover.Steps.Single(step => step.DebtId == CardId).StartsOn);
        Assert.Equal(20m, rollover.RecurringRoom);
    }

    [Fact]
    public void Track_LeavesOutAnotherCurrencyAndDoesNotTreatAMissingRateAsOpen()
    {
        var recovery = CashFlowRecovery.Track(PayoffRollover.Compare(Input(
            0m,
            [
                Debt(StoreId, "Store", 50m, 0m, 50m),
                Debt(CardId, "Unknown", 80m, null, 10m),
                Debt(VisaId, "Canada", 500m, 20m, 25m, currency: "CAD")
            ])));

        Assert.Equal(["CAD"], recovery.ExcludedCurrencies);
        Assert.Contains("CAD", recovery.Assumptions[^1], StringComparison.Ordinal);
        Assert.Equal(50m, recovery.Rollover.StartingObligation);
        Assert.Null(recovery.Rollover.RemainingObligation);
        Assert.Equal(1, recovery.Rollover.UnknownRemaining);
        Assert.Equal(50m, recovery.Rollover.RecurringRoom);
        Assert.Equal(0m, recovery.Rollover.ReleasedExtra);
        Assert.Contains("Unknown is missing a rate, minimum, or due date", recovery.Rollover.Explanation, StringComparison.Ordinal);
        Assert.Contains("50.00 USD a month from February 15, 2026", recovery.Rollover.Explanation, StringComparison.Ordinal);
    }

    [Fact]
    public void Track_MatchesRolloverWhenNothingIsReclaimedAndMatchesReclaimAllWhenTheAmountCoversIt()
    {
        var debts = new[]
        {
            Debt(StoreId, "Store", 50m, 0m, 50m),
            Debt(CardId, "Card", 100m, 12m, 25m)
        };
        var nothing = CashFlowRecovery.Track(PayoffRollover.Compare(Input(0m, debts)));
        var covered = CashFlowRecovery.Track(PayoffRollover.Compare(Input(0m, debts, reclaim: 10_000m)));

        Assert.Equal(0m, nothing.ReclaimAmount);
        Assert.Equal(nothing.Rollover.RecurringRoom, nothing.Reclaim.RecurringRoom);
        Assert.Equal(
            nothing.Rollover.Steps.Select(step => step.BreathingRoom),
            nothing.Reclaim.Steps.Select(step => step.BreathingRoom));

        Assert.Equal(covered.ReclaimAll.RecurringRoom, covered.Reclaim.RecurringRoom);
        Assert.Equal(
            covered.ReclaimAll.Steps.Select(step => step.StartsOn),
            covered.Reclaim.Steps.Select(step => step.StartsOn));
        Assert.Equal(
            covered.ReclaimAll.Steps.Select(step => step.BreathingRoom),
            covered.Reclaim.Steps.Select(step => step.BreathingRoom));
    }

    [Fact]
    public void Track_TreatsANegativeExtraAndANegativeReclaimAsNothing()
    {
        var recovery = CashFlowRecovery.Track(PayoffRollover.Compare(Input(
            -25m,
            [
                Debt(StoreId, "Store", 50m, 0m, 50m),
                Debt(CardId, "Card", 100m, 12m, 25m)
            ],
            reclaim: -10m)));

        Assert.Equal(0m, recovery.MonthlyExtra);
        Assert.Equal(0m, recovery.ReclaimAmount);
        Assert.Equal(recovery.Rollover.RecurringRoom, recovery.Reclaim.RecurringRoom);
        Assert.Equal(75m, recovery.Rollover.RecurringRoom);
    }

    [Fact]
    public void Track_KeepsTheFirstCopyAndDropsAZeroBalance()
    {
        var recovery = CashFlowRecovery.Track(PayoffRollover.Compare(Input(
            0m,
            [
                Debt(StoreId, "Store", 50m, 0m, 50m),
                Debt(StoreId, "Copy", 500m, 30m, 40m),
                Debt(CardId, "Card", 0m, 20m, 25m)
            ])));

        var step = Assert.Single(recovery.Rollover.Steps);
        Assert.Equal(StoreId, step.DebtId);
        Assert.Equal("Store", step.Name);
        Assert.Equal(new DateOnly(2026, 2, 15), step.StartsOn);
        Assert.Equal(50m, step.BreathingRoom);
        Assert.Equal(50m, recovery.Rollover.RecurringRoom);
        Assert.Equal(0m, recovery.Rollover.RemainingObligation);
    }

    [Fact]
    public void Track_FreesThePlannedMinimumWhenTheLastPaymentIsSmaller()
    {
        var recovery = CashFlowRecovery.Track(PayoffRollover.Compare(Input(
            0m,
            [
                Debt(StoreId, "Store", 30m, 0m, 50m, extra: 10m),
                Debt(CardId, "Card", 100m, 0m, 10m)
            ])));

        var store = recovery.Rollover.Steps.Single(step => step.DebtId == StoreId);
        Assert.Equal(new DateOnly(2026, 1, 15), store.EndedOn);
        Assert.Equal(new DateOnly(2026, 2, 15), store.StartsOn);
        Assert.Equal(60m, store.Amount);
        Assert.Equal(0m, store.BreathingRoom);
        Assert.Equal(new DateOnly(2026, 4, 15), recovery.Rollover.Steps.Single(step => step.DebtId == CardId).StartsOn);
        Assert.Equal(70m, recovery.Rollover.RecurringRoom);
        Assert.Contains("does not reduce either amount", recovery.Assumptions[1], StringComparison.Ordinal);
    }

    [Fact]
    public void Track_RepeatsTheSameDatesAndCents()
    {
        var input = Input(
            0m,
            [
                Debt(StoreId, "Store", 100m, 0m, 50m, extra: 10m),
                Debt(CardId, "Card", 200m, 0m, 25m)
            ],
            reclaim: 20m,
            order: [StoreId, CardId]);
        var first = CashFlowRecovery.Track(PayoffRollover.Compare(input));
        var again = CashFlowRecovery.Track(PayoffRollover.Compare(input));

        Assert.Equal(first.Rollover.Steps.Select(step => step.StartsOn), again.Rollover.Steps.Select(step => step.StartsOn));
        Assert.Equal(first.Rollover.Steps.Select(step => step.BreathingRoom), again.Rollover.Steps.Select(step => step.BreathingRoom));
        Assert.Equal(first.Rollover.RecurringRoom, again.Rollover.RecurringRoom);
        Assert.Equal(first.Reclaim.RecurringRoom, again.Reclaim.RecurringRoom);
        Assert.Equal(first.ReclaimAll.RecurringRoom, again.ReclaimAll.RecurringRoom);
        Assert.Equal(first.Rollover.Explanation, again.Rollover.Explanation);
        Assert.Equal(first.Reclaim.Explanation, again.Reclaim.Explanation);
        Assert.Equal(first.ReclaimAll.Explanation, again.ReclaimAll.Explanation);
        Assert.Equal(first.Assumptions, again.Assumptions);
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
