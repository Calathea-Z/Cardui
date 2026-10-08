using Cardui.Api.Domain.Accounts;
using Cardui.Api.Domain.Debts;
using Cardui.Api.Models;

namespace Cardui.Api.Domain.Recovery;

public static class PayoffProjection
{
    /// <summary>
    /// Projects one order month by month.
    /// Each debt pays its minimum and its own extra. Shared extra goes to the first debt in the order that can take it,
    /// then to the next debt in that same month when the first is paid off. Unused extra is not carried forward.
    /// A freed minimum is not added to the shared extra. When extraUntilUnderLimit is set, that debt receives shared extra
    /// only while it stays at or above 90 percent of its limit, and extra then follows avalanche.
    /// </summary>
    public static PayoffOrder Project(
        PayoffOrderKind kind,
        IReadOnlyList<PayoffDebt> order,
        IReadOnlyDictionary<Guid, int> avalancheIndex,
        decimal monthlyExtra,
        Guid? extraUntilUnderLimit = null)
    {
        var extra = monthlyExtra <= 0 ? 0 : AccountLedger.Round(monthlyExtra);
        var runs = Start(order, avalancheIndex);
        for (var step = 0; step < DebtRules.MaxRemainingTermMonths && runs.Any(run => !run.Finished); step++)
        {
            var pool = extra;
            foreach (var run in Sequence(runs, extraUntilUnderLimit))
            {
                if (!run.Finished)
                {
                    PayOne(run, step, ref pool);
                }
            }
        }

        foreach (var run in runs)
        {
            if (!run.Finished)
            {
                run.Stop = DebtScheduleStop.HorizonReached;
                run.Finished = true;
            }
        }

        return Finish(kind, runs);
    }

    #region Private Methods

    /// <summary>
    /// Opens one run per debt in the display order.
    /// The avalanche index puts extra back into that ranking once a utilization limit is met.
    /// </summary>
    private static List<PayoffRun> Start(
        IReadOnlyList<PayoffDebt> order,
        IReadOnlyDictionary<Guid, int> avalancheIndex)
    {
        var runs = new List<PayoffRun>(order.Count);
        foreach (var debt in order)
        {
            var index = avalancheIndex.TryGetValue(debt.Terms.DebtId, out var found)
                ? found
                : int.MaxValue;
            runs.Add(new PayoffRun(debt, DebtPaymentFacts.Resolve(debt.Terms), index));
        }

        return runs;
    }

    /// <summary>
    /// The order that receives shared extra this month.
    /// Without a limit target, it is the display order. With one, that debt stays first only while it is at the limit notice.
    /// After that, extra follows avalanche.
    /// </summary>
    private static IReadOnlyList<PayoffRun> Sequence(List<PayoffRun> runs, Guid? extraUntilUnderLimit)
    {
        if (extraUntilUnderLimit is not Guid debtId)
        {
            return runs;
        }

        var preferred = runs.FirstOrDefault(run => run.DebtId == debtId);
        if (preferred is not null && preferred.StillAtLimit())
        {
            return runs
                .OrderBy(run => run.DebtId == debtId ? 0 : 1)
                .ThenBy(run => run.AvalancheIndex)
                .ToList();
        }

        return runs.OrderBy(run => run.AvalancheIndex).ToList();
    }

    /// <summary>
    /// Charges one month and takes the minimum, this debt's extra, and any shared extra still in the pool.
    /// The debt stops when it is paid off, the balance does not fall, the rate is unknown, or the date cannot be stepped.
    /// </summary>
    private static void PayOne(PayoffRun run, int step, ref decimal pool)
    {
        if (!TryDueDate(run.Opening.DueDate, step, out var due) || due > DebtRules.LatestDate)
        {
            run.Stop = DebtScheduleStop.HorizonReached;
            run.Finished = true;
            return;
        }

        var (rate, promotional) = DebtRate.InEffect(
            run.Debt.Terms.Apr,
            run.Debt.Terms.PromotionalApr,
            run.Debt.Terms.PromotionalEndsOn,
            due);
        if (rate is not decimal ratePercent)
        {
            run.Stop = DebtScheduleStop.RateUnknown;
            run.Finished = true;
            return;
        }

        var period = DebtPeriodCalculator.Calculate(
            due,
            run.Balance,
            ratePercent,
            promotional,
            run.Opening.Minimum,
            run.OwnExtra + pool);
        pool -= SharedUsed(run.OwnExtra, period.ExtraPaid);
        run.Interest += period.Interest;
        run.Balance = period.EndingBalance;
        run.EndingUtilization = CurrentUtilization(run);
        if (run.WasHigh
            && run.PaymentsUntilUnderLimit is null
            && run.EndingUtilization is decimal utilization
            && utilization < DebtSummary.UtilizationLimitNotice)
        {
            run.PaymentsUntilUnderLimit = step + 1;
        }

        if (period.EndingBalance <= 0)
        {
            run.PaidOffOn = due;
            run.PaymentsUntilPaidOff = step + 1;
            run.Stop = DebtScheduleStop.PaidOff;
            run.Finished = true;
            return;
        }

        if (period.EndingBalance >= period.StartingBalance)
        {
            run.Stop = DebtScheduleStop.DoesNotPayDown;
            run.Finished = true;
        }
    }

    /// <summary>
    /// The part of the payment that came from the shared pool rather than this debt's own extra.
    /// </summary>
    private static decimal SharedUsed(decimal ownExtra, decimal extraPaid)
    {
        if (extraPaid <= ownExtra)
        {
            return 0;
        }

        return extraPaid - ownExtra;
    }

    /// <summary>
    /// Utilization after the latest balance change. An installment stays unknown.
    /// </summary>
    private static decimal? CurrentUtilization(PayoffRun run)
    {
        if (run.Debt.Terms.Kind != DebtKind.Revolving)
        {
            return null;
        }

        return DebtRules.Utilization(run.Balance, run.Debt.CreditLimit);
    }

    /// <summary>
    /// The due date that many months after the first, counted from the first date each time.
    /// A month that cannot be represented ends the projection. This matches debt amortization.
    /// </summary>
    private static bool TryDueDate(DateOnly firstDue, int monthsLater, out DateOnly due)
    {
        try
        {
            due = firstDue.AddMonths(monthsLater);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            due = default;
            return false;
        }
    }

    /// <summary>
    /// Assembles the order from the runs.
    /// PaidOffOn is set only when every calculable debt has cleared.
    /// </summary>
    private static PayoffOrder Finish(PayoffOrderKind kind, List<PayoffRun> runs)
    {
        var debts = runs.Select(Outcome).ToList();
        return new PayoffOrder(
            kind,
            debts.Select(debt => debt.DebtId).ToList(),
            runs.Sum(run => run.Interest),
            PlanPaidOff(runs),
            debts);
    }

    /// <summary>
    /// The latest payoff, or null when a calculable debt is still open or could not be calculated.
    /// </summary>
    private static DateOnly? PlanPaidOff(List<PayoffRun> runs)
    {
        DateOnly? last = null;
        foreach (var run in runs)
        {
            if (!run.Opening.IsResolved || run.PaidOffOn is not DateOnly paid)
            {
                return null;
            }

            if (last is null || paid > last)
            {
                last = paid;
            }
        }

        return last;
    }

    /// <summary>
    /// Copies one run into the result shape.
    /// PaymentsUntilUnderLimit stays empty unless the debt started at or above the limit notice.
    /// </summary>
    private static PayoffDebtOutcome Outcome(PayoffRun run)
    {
        var openingUtilization = run.Debt.Terms.Kind == DebtKind.Revolving
            ? DebtRules.Utilization(run.Opening.Balance, run.Debt.CreditLimit)
            : null;
        return new PayoffDebtOutcome(
            run.DebtId,
            run.Debt.Terms.Name,
            run.Stop ?? DebtScheduleStop.HorizonReached,
            run.Opening.IsResolved ? run.Opening.RatePercent : null,
            run.Opening.Balance,
            run.Opening.IsResolved ? run.Opening.Minimum : null,
            openingUtilization,
            run.Interest,
            run.Balance,
            run.PaidOffOn,
            run.PaymentsUntilPaidOff,
            run.WasHigh ? run.PaymentsUntilUnderLimit : null,
            run.EndingUtilization ?? openingUtilization);
    }

    #endregion
}
