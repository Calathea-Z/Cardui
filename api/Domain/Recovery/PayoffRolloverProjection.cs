using Cardui.Api.Domain.Accounts;
using Cardui.Api.Domain.Debts;
using Cardui.Api.Models;

namespace Cardui.Api.Domain.Recovery;

public static class PayoffRolloverProjection
{
    /// <summary>
    /// Projects one order month by month, then applies cash freed by a paid-off debt.
    /// Each open debt pays its minimum and its own extra. Shared extra goes to the first debt that can take it,
    /// including leftover in the same month, and unused extra is not carried forward.
    /// A freed minimum and that debt's planned extra join that extra on the following month, after the payment that ends the debt.
    /// A null reclaim keeps every freed dollar. Zero rolls all of it. A positive amount keeps that much each month and rolls the rest.
    /// The interest difference and the explanation stay empty here. The comparison fills them once all three paths exist.
    /// </summary>
    public static PayoffRolloverPath Project(
        PayoffRolloverKind kind,
        IReadOnlyList<PayoffDebt> order,
        decimal monthlyExtra,
        decimal? reclaimAmount)
    {
        var extra = monthlyExtra <= 0 ? 0 : AccountLedger.Round(monthlyExtra);
        var reclaim = reclaimAmount is > 0 ? AccountLedger.Round(reclaimAmount.Value) : reclaimAmount;
        var runs = order.Select(debt => new PayoffRolloverRun(debt)).ToList();
        decimal cashReclaimed = 0;
        decimal cashRolled = 0;
        for (var step = 0; step < DebtRules.MaxRemainingTermMonths && runs.Any(run => !run.Finished); step++)
        {
            var freed = FreedCash(runs, step);
            var kept = Kept(freed, reclaim);
            cashReclaimed += kept;
            PayRound(runs, step, extra, freed - kept, ref cashRolled);
        }

        foreach (var run in runs)
        {
            if (!run.Finished)
            {
                run.Stop = DebtScheduleStop.HorizonReached;
                run.Finished = true;
            }
        }

        return Finish(kind, runs, cashReclaimed, cashRolled);
    }

    #region Private Methods

    /// <summary>
    /// The planned minimum and planned extra of debts whose payment ended on an earlier round.
    /// The round that clears a debt still pays that debt, so its cash is not free yet.
    /// </summary>
    private static decimal FreedCash(IReadOnlyList<PayoffRolloverRun> runs, int step)
    {
        decimal freed = 0;
        foreach (var run in runs)
        {
            if (run.Stop == DebtScheduleStop.PaidOff
                && run.PaymentsUntilPaidOff is int paid
                && paid <= step)
            {
                freed += run.Opening.Minimum + run.OwnExtra;
            }
        }

        return AccountLedger.Round(freed);
    }

    /// <summary>
    /// The freed cash held back this month.
    /// A null reclaim keeps the whole amount. A smaller request keeps that request, and never more than the cash that is free.
    /// </summary>
    private static decimal Kept(decimal freed, decimal? reclaim)
    {
        if (freed <= 0)
        {
            return 0;
        }

        if (reclaim is null || reclaim.Value >= freed)
        {
            return freed;
        }

        return reclaim.Value;
    }

    /// <summary>
    /// Pays every open debt once.
    /// Shared extra is used before rolled cash. Rolled cash the first debt does not take continues to the next open debt in the same round.
    /// </summary>
    private static void PayRound(
        IReadOnlyList<PayoffRolloverRun> runs,
        int step,
        decimal sharedExtra,
        decimal roll,
        ref decimal cashRolled)
    {
        var sharedLeft = sharedExtra;
        var rollLeft = roll;
        foreach (var run in runs)
        {
            if (!run.Finished)
            {
                PayOne(run, step, ref sharedLeft, ref rollLeft, ref cashRolled);
            }
        }
    }

    /// <summary>
    /// Charges one month and takes the minimum, this debt's extra, shared extra, and rolled cash still available.
    /// The debt stops when it is paid off, the balance does not fall, the rate is unknown, or the date cannot be stepped.
    /// </summary>
    private static void PayOne(
        PayoffRolloverRun run,
        int step,
        ref decimal sharedLeft,
        ref decimal rollLeft,
        ref decimal cashRolled)
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
            run.OwnExtra + sharedLeft + rollLeft);
        ApplyExtra(period.ExtraPaid, run.OwnExtra, ref sharedLeft, ref rollLeft, ref cashRolled);
        run.Interest += period.Interest;
        run.Balance = period.EndingBalance;
        run.EndingUtilization = CurrentUtilization(run);
        run.BalancePoints.Add(new PayoffBalancePoint(
            run.DebtId,
            due,
            period.EndingBalance <= 0 ? 0 : period.EndingBalance,
            period.Interest,
            period.Payment));
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
    /// Splits extra paid above this debt's own extra into shared extra and rolled cash.
    /// Own extra is counted first. Shared extra is counted next. The remainder is rolled cash that landed on this debt.
    /// </summary>
    private static void ApplyExtra(
        decimal extraPaid,
        decimal ownExtra,
        ref decimal sharedLeft,
        ref decimal rollLeft,
        ref decimal cashRolled)
    {
        var beyondOwn = extraPaid > ownExtra ? extraPaid - ownExtra : 0;
        var sharedUsed = beyondOwn > sharedLeft ? sharedLeft : beyondOwn;
        var rollUsed = beyondOwn - sharedUsed;
        sharedLeft -= sharedUsed;
        rollLeft -= rollUsed;
        cashRolled += rollUsed;
    }

    /// <summary>
    /// Utilization after the latest balance change. An installment stays unknown.
    /// </summary>
    private static decimal? CurrentUtilization(PayoffRolloverRun run)
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
    /// Assembles the path from the runs.
    /// PaidOffOn is set only when every calculable debt has cleared. InterestDifference and the explanation are filled by the comparison.
    /// </summary>
    private static PayoffRolloverPath Finish(
        PayoffRolloverKind kind,
        List<PayoffRolloverRun> runs,
        decimal cashReclaimed,
        decimal cashRolled)
    {
        var debts = runs.Select(Outcome).ToList();
        return new PayoffRolloverPath(
            kind,
            debts.Select(debt => debt.DebtId).ToList(),
            runs.Sum(run => run.Interest),
            PlanPaidOff(runs),
            debts,
            FreedPayments(runs),
            BalancePoints(runs),
            cashReclaimed,
            cashRolled,
            0,
            "");
    }

    /// <summary>
    /// The latest payoff, or null when a calculable debt is still open or could not be calculated.
    /// </summary>
    private static DateOnly? PlanPaidOff(List<PayoffRolloverRun> runs)
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
    /// One freed payment for each debt that reached zero, in the order those payments ended.
    /// </summary>
    private static IReadOnlyList<PayoffFreedPayment> FreedPayments(List<PayoffRolloverRun> runs)
    {
        return runs
            .Where(run => run.Stop == DebtScheduleStop.PaidOff && run.PaidOffOn is DateOnly)
            .OrderBy(run => run.PaidOffOn)
            .ThenBy(run => run.Debt.Terms.Name, StringComparer.Ordinal)
            .ThenBy(run => run.DebtId)
            .Select(run => new PayoffFreedPayment(
                run.DebtId,
                run.Debt.Terms.Name,
                run.PaidOffOn!.Value,
                RemovalDate(run),
                run.Opening.Minimum,
                run.OwnExtra,
                AccountLedger.Round(run.Opening.Minimum + run.OwnExtra)))
            .ToList();
    }

    /// <summary>
    /// Every debt's balance after each payment, by due date and then in payoff order.
    /// A chart can stack these without sorting them again.
    /// </summary>
    private static IReadOnlyList<PayoffBalancePoint> BalancePoints(List<PayoffRolloverRun> runs)
    {
        return runs
            .SelectMany((run, index) => run.BalancePoints.Select(point => (point, index)))
            .OrderBy(entry => entry.point.DueDate)
            .ThenBy(entry => entry.index)
            .Select(entry => entry.point)
            .ToList();
    }

    /// <summary>
    /// The next due date after the payoff, counted from the first due date.
    /// That is when the minimum is no longer paid. The payoff month still pays the debt.
    /// Null when the date cannot be represented or is after the latest date the debt rules allow.
    /// </summary>
    private static DateOnly? RemovalDate(PayoffRolloverRun run)
    {
        if (run.PaymentsUntilPaidOff is not int paid
            || !TryDueDate(run.Opening.DueDate, paid, out var starts)
            || starts > DebtRules.LatestDate)
        {
            return null;
        }

        return starts;
    }

    /// <summary>
    /// Copies one run into the result shape.
    /// PaymentsUntilUnderLimit stays empty. This projection does not move a debt to lower utilization.
    /// </summary>
    private static PayoffDebtOutcome Outcome(PayoffRolloverRun run)
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
            null,
            run.EndingUtilization ?? openingUtilization);
    }

    #endregion
}
