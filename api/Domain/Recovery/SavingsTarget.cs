using Cardui.Api.Domain.Accounts;

namespace Cardui.Api.Domain.Recovery;

public static class SavingsTarget
{
    public const int MaxMonths = 600;

    /// <summary>
    /// Plans how a target is reserved from a start date.
    /// Remaining is the target minus what is already reserved, and it is never negative.
    /// When a target date is in range, ToHitDate splits Remaining across those months and trues up the last month.
    /// When a monthly contribution is given, FromContribution applies it until Remaining is met.
    /// Neither path spends the money. A date already past reports the gap due at the start.
    /// A path that cannot finish within 600 months sets its horizon flag and contributes nothing for that path.
    /// </summary>
    public static SavingsTargetPlan Plan(
        decimal alreadyReserved,
        decimal targetAmount,
        DateOnly from,
        DateOnly? targetDate,
        decimal? monthlyContribution)
    {
        var remaining = Remaining(alreadyReserved, targetAmount);
        if (remaining == 0)
        {
            return Met();
        }

        var dated = DatesForTarget(from, targetDate, remaining);
        var contributed = ContributionsFromAmount(from, remaining, monthlyContribution);
        return new SavingsTargetPlan(
            remaining,
            false,
            dated.DatePassed,
            dated.BeyondHorizon,
            contributed.DoesNotReach,
            dated.Level,
            dated.Final,
            contributed.ReachesOn,
            dated.Contributions,
            contributed.Contributions);
    }

    #region Private Methods

    /// <summary>
    /// The gap still to reserve, in cents.
    /// An amount already at or above the target leaves no gap. A negative input is treated as zero.
    /// </summary>
    private static decimal Remaining(decimal alreadyReserved, decimal targetAmount)
    {
        var reserved = alreadyReserved <= 0 ? 0 : AccountLedger.Round(alreadyReserved);
        var target = targetAmount <= 0 ? 0 : AccountLedger.Round(targetAmount);
        return target > reserved ? target - reserved : 0;
    }

    /// <summary>
    /// A target that is already reserved.
    /// </summary>
    private static SavingsTargetPlan Met()
    {
        return new SavingsTargetPlan(
            0,
            true,
            false,
            false,
            false,
            null,
            null,
            null,
            [],
            []);
    }

    /// <summary>
    /// The monthly amounts that meet the target on its date.
    /// A missing date leaves this path empty. A past date is one amount due at the start.
    /// </summary>
    private static (
        bool DatePassed,
        bool BeyondHorizon,
        decimal? Level,
        decimal? Final,
        IReadOnlyList<SavingsContribution> Contributions) DatesForTarget(
        DateOnly from,
        DateOnly? targetDate,
        decimal remaining)
    {
        if (targetDate is not DateOnly target)
        {
            return (false, false, null, null, []);
        }

        if (target < from)
        {
            var dueNow = new SavingsContribution(from, remaining);
            return (true, false, remaining, remaining, [dueNow]);
        }

        var dates = DatesThrough(from, target, out var beyond);
        if (beyond || dates.Count == 0)
        {
            return (false, true, null, null, []);
        }

        var split = Split(remaining, dates);
        return (false, false, split.Level, split.Final, split.Contributions);
    }

    /// <summary>
    /// Month starts from the anchor through the target date, including both.
    /// Beyond is true when the 600 month cap is reached while the target is still later.
    /// </summary>
    private static List<DateOnly> DatesThrough(DateOnly from, DateOnly target, out bool beyond)
    {
        var dates = new List<DateOnly>();
        beyond = false;
        for (var step = 0; step < MaxMonths; step++)
        {
            if (!TryMonth(from, step, out var date))
            {
                return dates;
            }

            if (date > target)
            {
                return dates;
            }

            dates.Add(date);
        }

        if (TryMonth(from, MaxMonths, out var next) && next <= target)
        {
            beyond = true;
        }

        return dates;
    }

    /// <summary>
    /// Splits a gap across known dates.
    /// Earlier months use the rounded share. The last month is whatever is left, so the sum equals the gap.
    /// A share that rounds to more than the gap is reduced, and a final zero month is dropped.
    /// </summary>
    private static (decimal Level, decimal Final, IReadOnlyList<SavingsContribution> Contributions) Split(
        decimal remaining,
        IReadOnlyList<DateOnly> dates)
    {
        var level = AccountLedger.Round(remaining / dates.Count);
        var amounts = new List<decimal>(dates.Count);
        var consumed = 0m;
        for (var index = 0; index < dates.Count - 1; index++)
        {
            var amount = level;
            if (consumed + amount > remaining)
            {
                amount = remaining - consumed;
            }

            amounts.Add(amount);
            consumed += amount;
        }

        amounts.Add(remaining - consumed);
        while (amounts.Count > 1 && amounts[^1] == 0)
        {
            amounts.RemoveAt(amounts.Count - 1);
        }

        var contributions = new List<SavingsContribution>(amounts.Count);
        for (var index = 0; index < amounts.Count; index++)
        {
            if (amounts[index] <= 0)
            {
                continue;
            }

            contributions.Add(new SavingsContribution(dates[index], amounts[index]));
        }

        return (amounts[0], amounts[^1], contributions);
    }

    /// <summary>
    /// Applies a chosen monthly amount until the gap is gone.
    /// A missing or zero amount does not mean the contribution failed. An amount that cannot finish
    /// within 600 months sets the horizon flag and returns no partial list.
    /// </summary>
    private static (
        bool DoesNotReach,
        DateOnly? ReachesOn,
        IReadOnlyList<SavingsContribution> Contributions) ContributionsFromAmount(
        DateOnly from,
        decimal remaining,
        decimal? monthlyContribution)
    {
        if (monthlyContribution is not decimal offered || offered <= 0)
        {
            return (false, null, []);
        }

        var monthly = AccountLedger.Round(offered);
        if (monthly <= 0)
        {
            return (true, null, []);
        }

        var left = remaining;
        var items = new List<SavingsContribution>();
        for (var step = 0; step < MaxMonths; step++)
        {
            if (!TryMonth(from, step, out var date))
            {
                return (true, null, []);
            }

            var amount = monthly >= left ? left : monthly;
            items.Add(new SavingsContribution(date, amount));
            left -= amount;
            if (left <= 0)
            {
                return (false, date, items);
            }
        }

        return (true, null, []);
    }

    /// <summary>
    /// A date that many months after the start, counted from the start each time.
    /// </summary>
    private static bool TryMonth(DateOnly from, int monthsLater, out DateOnly date)
    {
        try
        {
            date = from.AddMonths(monthsLater);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            date = default;
            return false;
        }
    }

    #endregion
}
