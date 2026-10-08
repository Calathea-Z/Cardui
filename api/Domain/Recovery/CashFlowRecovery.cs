using Cardui.Api.Domain.Accounts;

namespace Cardui.Api.Domain.Recovery;

public static class CashFlowRecovery
{
    /// <summary>
    /// Shows when each payoff removes a monthly obligation, and the recurring breathing room that follows.
    /// The month that pays a debt off still pays it. The minimum is removed on the next due date.
    /// While a later debt is still being paid, rollover keeps the freed cash committed, reclaim keeps the
    /// requested amount each month, and reclaiming all keeps every freed dollar. When no debt can take
    /// another payment, freed cash that had been rolling becomes breathing room. Shared extra joins that
    /// room only once every debt is paid off. The same comparison produces the same dates and the same cents.
    /// </summary>
    public static CashFlowRecoveryReport Track(PayoffRolloverComparison comparison)
    {
        return new CashFlowRecoveryReport(
            comparison.PlanningCurrency,
            comparison.MonthlyExtra,
            comparison.ReclaimAmount,
            Describe(comparison.Rollover, comparison.MonthlyExtra, comparison.ReclaimAmount, comparison.PlanningCurrency),
            Describe(comparison.Reclaim, comparison.MonthlyExtra, comparison.ReclaimAmount, comparison.PlanningCurrency),
            Describe(comparison.ReclaimAll, comparison.MonthlyExtra, comparison.ReclaimAmount, comparison.PlanningCurrency),
            comparison.ExcludedCurrencies,
            Assumptions(comparison.PlanningCurrency, comparison.ExcludedCurrencies));
    }

    #region Private Methods

    /// <summary>
    /// Builds one path's steps, the obligations still due, and the explanation.
    /// Shared extra is released only when the rollover path paid every debt off.
    /// </summary>
    private static CashFlowRecoveryPath Describe(
        PayoffRolloverPath source,
        decimal monthlyExtra,
        decimal reclaim,
        string currency)
    {
        var steps = Steps(source, reclaim);
        var (starting, _) = Minimums(source.Debts);
        var (remaining, unknown) = Minimums(source.Debts.Where(debt => debt.Stop != DebtScheduleStop.PaidOff));
        var breathing = steps.Count == 0 ? 0 : steps[^1].BreathingRoom;
        var released = source.PaidOffOn is null ? 0 : monthlyExtra;
        var recurring = AccountLedger.Round(breathing + released);
        return new CashFlowRecoveryPath(
            source.Kind,
            steps,
            starting,
            remaining,
            unknown,
            breathing,
            released,
            recurring,
            CashFlowRecoveryExplanation.Describe(
                starting,
                steps,
                source.Debts,
                released,
                recurring,
                currency));
    }

    /// <summary>
    /// One step per paid-off debt, in the order the obligations are removed.
    /// Breathing room grows by the freed cash that is no longer committed after that step.
    /// </summary>
    private static List<CashFlowRecoveryStep> Steps(PayoffRolloverPath source, decimal reclaim)
    {
        var byId = source.Debts.ToDictionary(debt => debt.DebtId);
        var steps = new List<CashFlowRecoveryStep>();
        decimal freedSoFar = 0;
        decimal room = 0;
        foreach (var payment in InTime(source.FreedPayments))
        {
            if (!byId.TryGetValue(payment.DebtId, out var outcome))
            {
                continue;
            }

            freedSoFar = AccountLedger.Round(freedSoFar + payment.Amount);
            var next = StillCommitted(outcome.PaymentsUntilPaidOff ?? 0, payment.DebtId, source.Debts)
                ? RoomWhileOpen(source.Kind, freedSoFar, reclaim)
                : freedSoFar;
            var added = AccountLedger.Round(next - room);
            room = next;
            steps.Add(new CashFlowRecoveryStep(
                payment.DebtId,
                payment.Name,
                payment.EndedOn,
                payment.StartsOn,
                payment.Minimum,
                payment.Extra,
                payment.Amount,
                added,
                room));
        }

        return steps;
    }

    /// <summary>
    /// Freed payments in the order their minimums are removed.
    /// A missing removal date is listed after the dated ones.
    /// </summary>
    private static IReadOnlyList<PayoffFreedPayment> InTime(IReadOnlyList<PayoffFreedPayment> payments)
    {
        return payments
            .OrderBy(payment => payment.StartsOn ?? DateOnly.MaxValue)
            .ThenBy(payment => payment.EndedOn)
            .ThenBy(payment => payment.Name, StringComparer.Ordinal)
            .ThenBy(payment => payment.DebtId)
            .ToList();
    }

    /// <summary>
    /// True when some other debt will still take a payment after this freed cash starts.
    /// A debt at the month cap still takes it. A debt that stopped without paying off does not.
    /// </summary>
    private static bool StillCommitted(
        int startStep,
        Guid debtId,
        IReadOnlyList<PayoffDebtOutcome> debts)
    {
        foreach (var debt in debts)
        {
            if (debt.DebtId == debtId)
            {
                continue;
            }

            if (debt.Stop == DebtScheduleStop.HorizonReached)
            {
                return true;
            }

            if (debt.Stop == DebtScheduleStop.PaidOff
                && debt.PaymentsUntilPaidOff is int paid
                && paid > startStep)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// The freed cash kept each month while a later debt can still take a payment.
    /// Rollover keeps none. Reclaiming all keeps the freed cash. A partial reclaim keeps the request, and no more than the cash that is free.
    /// </summary>
    private static decimal RoomWhileOpen(PayoffRolloverKind kind, decimal freed, decimal reclaim)
    {
        if (freed <= 0)
        {
            return 0;
        }

        if (kind == PayoffRolloverKind.ReclaimAll)
        {
            return freed;
        }

        if (kind == PayoffRolloverKind.Reclaim && reclaim > 0)
        {
            return reclaim >= freed ? freed : reclaim;
        }

        return 0;
    }

    /// <summary>
    /// The sum of known minimums, and how many debts in the list had no known minimum.
    /// The sum is null when every debt in the list is unknown. An empty list is zero.
    /// </summary>
    private static (decimal? Obligation, int Unknown) Minimums(IEnumerable<PayoffDebtOutcome> debts)
    {
        var unknown = 0;
        var known = 0m;
        var knownCount = 0;
        foreach (var debt in debts)
        {
            if (debt.Minimum is not decimal minimum)
            {
                unknown++;
                continue;
            }

            known += minimum;
            knownCount++;
        }

        if (knownCount == 0 && unknown > 0)
        {
            return (null, unknown);
        }

        return (AccountLedger.Round(known), unknown);
    }

    /// <summary>
    /// The obligation and breathing-room rules this report used.
    /// </summary>
    private static IReadOnlyList<string> Assumptions(string currency, IReadOnlyList<string> excluded)
    {
        var assumptions = new List<string>
        {
            "Each payoff removes that debt's minimum on the next monthly due date. The month that pays the debt off still pays it.",
            "The planned extra on that debt stops with the minimum. A last payment that was smaller, because the balance was smaller, does not reduce either amount. Shared extra is not part of the freed payment.",
            "While a later debt is still being paid, the freed payment stays committed. Reclaiming keeps the requested amount of that freed cash each month, and reclaiming all keeps every freed dollar. Rollover keeps none of it back.",
            "A debt still open at the month cap keeps that cash committed. A debt that does not pay down, or one missing a rate, minimum, or due date, does not take the cash.",
            "When no debt can take another payment, the freed cash is recurring breathing room, including cash that had been rolling. Once every debt is paid off, shared extra is no longer sent and is included in that room.",
            "The same inputs produce the same dates and the same cents."
        };
        if (excluded.Count > 0)
        {
            assumptions.Add(
                "Amounts in another currency are left out: "
                + string.Join(", ", excluded)
                + ". This recovery uses "
                + currency.Trim().ToUpperInvariant()
                + " only.");
        }

        return assumptions;
    }

    #endregion
}
