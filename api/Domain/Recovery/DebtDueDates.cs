using Cardui.Api.Domain.Debts;

namespace Cardui.Api.Domain.Recovery;

internal static class DebtDueDates
{
    /// <summary>
    /// The due date that many months after the first, counted from the first date each time,
    /// so January 31 is followed by February 28 and then March 31.
    /// False when that month cannot be represented.
    /// </summary>
    public static bool TryStep(DateOnly firstDue, int monthsLater, out DateOnly due)
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
    /// The first month index whose due date is on or after from.
    /// A stored due date before from is not replayed; the schedule starts at that later month.
    /// Null when the 600 month cap from the stored due date still falls before from. A null from starts at the stored date.
    /// </summary>
    public static int? FirstIndexOnOrAfter(DateOnly firstDue, DateOnly? from)
    {
        if (from is not DateOnly start || firstDue >= start)
        {
            return 0;
        }

        for (var index = 1; index < DebtRules.MaxRemainingTermMonths; index++)
        {
            if (!TryStep(firstDue, index, out var due))
            {
                return null;
            }

            if (due >= start)
            {
                return index;
            }
        }

        return null;
    }
}
