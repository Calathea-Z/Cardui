namespace Cardui.Api.Domain;

public static class CategoryTargetCalculator
{
    /// <summary>
    /// Builds each category's target, rolled amount, spent, and remaining.
    /// Rollover comes from the previous calendar month only, and only when that month saved rollover.
    /// A gap, or a month with no target for the category, resets the carry.
    /// Spent amounts are already classified: income, transfers, and statement adjustments are not included.
    /// </summary>
    public static IReadOnlyList<CategoryTargetProgress> Calculate(
        IReadOnlyList<CategoryTargetAssignment> assignments,
        IReadOnlyList<CategoryTargetHistoryMonth> earlierMonths,
        IReadOnlyList<CategoryMonthSpent> spentThisMonth,
        IReadOnlyList<Guid> categoryIds,
        int year,
        int month)
    {
        var lines = new List<CategoryTargetProgress>(categoryIds.Count);
        var seen = new HashSet<Guid>(categoryIds);

        foreach (var categoryId in categoryIds)
        {
            var assignment = assignments.FirstOrDefault(item => item.CategoryId == categoryId);
            lines.Add(Line(
                categoryId,
                assignment,
                RolloverIn(earlierMonths, year, month, categoryId),
                SpentFor(spentThisMonth, categoryId)));
        }

        foreach (var spent in spentThisMonth)
        {
            if (spent.Spent == 0)
            {
                continue;
            }

            if (spent.CategoryId is Guid categoryId && seen.Contains(categoryId))
            {
                continue;
            }

            lines.Add(Line(spent.CategoryId, null, 0, spent.Spent));
        }

        return lines;
    }

    /// <summary>
    /// Copies target amounts and rollover choices into a later month.
    /// Leftover money is not copied. It arrives only through rollover.
    /// </summary>
    public static IReadOnlyList<CategoryTargetAssignment> CopyForward(
        IReadOnlyList<CategoryTargetAssignment> source)
    {
        return source
            .GroupBy(item => item.CategoryId)
            .Select(group => group.First())
            .Select(item => new CategoryTargetAssignment(
                item.CategoryId,
                item.Amount,
                item.Rollover))
            .ToList();
    }

    /// <summary>
    /// The first and last calendar day of a month.
    /// </summary>
    public static (DateOnly Start, DateOnly End) MonthBounds(int year, int month)
    {
        var start = new DateOnly(year, month, 1);
        return (start, start.AddMonths(1).AddDays(-1));
    }

    /// <summary>
    /// The last date whose spending counts for a month.
    /// The current month stops today. A past month runs through its last day.
    /// A later month has not started, so the end falls before the month begins.
    /// </summary>
    public static DateOnly SpendingEnd(int year, int month, DateOnly today)
    {
        var (start, last) = MonthBounds(year, month);
        if (start > today)
        {
            return today;
        }

        return last < today ? last : today;
    }

    #region Private Methods

    /// <summary>
    /// One category line. Available and remaining exist only when a target is set.
    /// </summary>
    private static CategoryTargetProgress Line(
        Guid? categoryId,
        CategoryTargetAssignment? assignment,
        decimal rolloverIn,
        decimal spent)
    {
        if (assignment is null)
        {
            return new CategoryTargetProgress(
                categoryId,
                Target: null,
                Rollover: false,
                rolloverIn,
                spent,
                Available: null,
                Remaining: null);
        }

        var available = assignment.Amount + rolloverIn;
        return new CategoryTargetProgress(
            categoryId,
            assignment.Amount,
            assignment.Rollover,
            rolloverIn,
            spent,
            available,
            available - spent);
    }

    /// <summary>
    /// The amount carried into this month for one category.
    /// The carry is the previous calendar month's remaining, and only when rollover was on.
    /// </summary>
    private static decimal RolloverIn(
        IReadOnlyList<CategoryTargetHistoryMonth> earlierMonths,
        int year,
        int month,
        Guid categoryId)
    {
        var ordered = earlierMonths
            .GroupBy(item => (item.Year, item.Month))
            .Select(group => group.Last())
            .OrderBy(item => item.Year)
            .ThenBy(item => item.Month)
            .ToList();

        if (ordered.Count == 0)
        {
            return 0;
        }

        decimal carry = 0;
        CategoryTargetHistoryMonth? previous = null;
        foreach (var history in ordered)
        {
            if (previous is not null && !IsNextCalendarMonth(previous, history))
            {
                carry = 0;
            }

            carry = NextCarry(history, categoryId, carry);
            previous = history;
        }

        if (previous is null || !IsImmediatelyBefore(previous, year, month))
        {
            return 0;
        }

        return carry;
    }

    /// <summary>
    /// The carry leaving one month. No target, or rollover off, leaves nothing for the next month.
    /// </summary>
    private static decimal NextCarry(
        CategoryTargetHistoryMonth history,
        Guid categoryId,
        decimal carry)
    {
        var assignment = history.Assignments.FirstOrDefault(item => item.CategoryId == categoryId);
        if (assignment is null || !assignment.Rollover)
        {
            return 0;
        }

        return assignment.Amount + carry - SpentFor(history.Spent, categoryId);
    }

    /// <summary>
    /// Posted spending for one category. Several rows for the same category are added.
    /// </summary>
    private static decimal SpentFor(IReadOnlyList<CategoryMonthSpent> spent, Guid? categoryId)
    {
        return spent
            .Where(item => item.CategoryId == categoryId)
            .Sum(item => item.Spent);
    }

    /// <summary>
    /// True when the second month is the calendar month after the first.
    /// </summary>
    private static bool IsNextCalendarMonth(
        CategoryTargetHistoryMonth previous,
        CategoryTargetHistoryMonth current)
    {
        return IsImmediatelyBefore(previous, current.Year, current.Month);
    }

    /// <summary>
    /// True when the history month is the calendar month before the requested one.
    /// </summary>
    private static bool IsImmediatelyBefore(
        CategoryTargetHistoryMonth history,
        int year,
        int month)
    {
        var nextYear = history.Month == 12 ? history.Year + 1 : history.Year;
        var nextMonth = history.Month == 12 ? 1 : history.Month + 1;
        return nextYear == year && nextMonth == month;
    }

    #endregion
}
