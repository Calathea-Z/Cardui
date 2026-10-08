using Cardui.Api.Models;

namespace Cardui.Api.Domain;

public static class CadenceDates
{
    /// <summary>
    /// Lists dates from the anchor through the latest date, using that step.
    /// Each month step starts from the anchor again, so a short month does not move later dates.
    /// </summary>
    public static IEnumerable<DateOnly> Enumerate(CadenceStep step, DateOnly anchor, DateOnly latest)
    {
        switch (step)
        {
            case CadenceStep.Weekly:
                for (var date = anchor; date <= latest; date = date.AddDays(7))
                {
                    yield return date;
                }

                yield break;
            case CadenceStep.Biweekly:
                for (var date = anchor; date <= latest; date = date.AddDays(14))
                {
                    yield return date;
                }

                yield break;
            case CadenceStep.Monthly:
                foreach (var date in MonthSteps(anchor, 1, latest))
                {
                    yield return date;
                }

                yield break;
            case CadenceStep.Quarterly:
                foreach (var date in MonthSteps(anchor, 3, latest))
                {
                    yield return date;
                }

                yield break;
            case CadenceStep.Yearly:
                foreach (var date in MonthSteps(anchor, 12, latest))
                {
                    yield return date;
                }

                yield break;
            case CadenceStep.Semimonthly:
                foreach (var date in SemimonthlyDates(anchor, latest))
                {
                    yield return date;
                }

                yield break;
            default:
                yield break;
        }
    }

    /// <summary>
    /// Maps an income cadence onto a repeating step.
    /// Irregular has no step.
    /// </summary>
    public static bool TryStep(IncomeCadence cadence, out CadenceStep step)
    {
        switch (cadence)
        {
            case IncomeCadence.Weekly:
                step = CadenceStep.Weekly;
                return true;
            case IncomeCadence.Biweekly:
                step = CadenceStep.Biweekly;
                return true;
            case IncomeCadence.Semimonthly:
                step = CadenceStep.Semimonthly;
                return true;
            case IncomeCadence.Monthly:
                step = CadenceStep.Monthly;
                return true;
            case IncomeCadence.Quarterly:
                step = CadenceStep.Quarterly;
                return true;
            case IncomeCadence.Yearly:
                step = CadenceStep.Yearly;
                return true;
            default:
                step = default;
                return false;
        }
    }

    /// <summary>
    /// Maps a bill cadence onto a repeating step.
    /// Irregular has no step.
    /// </summary>
    public static bool TryStep(ObligationCadence cadence, out CadenceStep step)
    {
        switch (cadence)
        {
            case ObligationCadence.Weekly:
                step = CadenceStep.Weekly;
                return true;
            case ObligationCadence.Biweekly:
                step = CadenceStep.Biweekly;
                return true;
            case ObligationCadence.Semimonthly:
                step = CadenceStep.Semimonthly;
                return true;
            case ObligationCadence.Monthly:
                step = CadenceStep.Monthly;
                return true;
            case ObligationCadence.Quarterly:
                step = CadenceStep.Quarterly;
                return true;
            case ObligationCadence.Yearly:
                step = CadenceStep.Yearly;
                return true;
            default:
                step = default;
                return false;
        }
    }

    #region Private Methods

    /// <summary>
    /// Adds the same number of months from the anchor for each step.
    /// A day the target month lacks, such as January 31 in February, uses that month's last day.
    /// The following step starts from the anchor again, so March 31 returns after February 28.
    /// </summary>
    private static IEnumerable<DateOnly> MonthSteps(DateOnly anchor, int monthsPerStep, DateOnly latest)
    {
        for (var step = 0; ; step++)
        {
            DateOnly date;
            try
            {
                date = anchor.AddMonths(step * monthsPerStep);
            }
            catch (ArgumentOutOfRangeException)
            {
                yield break;
            }

            if (date > latest)
            {
                yield break;
            }

            yield return date;
        }
    }

    /// <summary>
    /// Two dates each month, fifteen days apart, using the anchor's day as one of them.
    /// A day past the end of a short month uses that month's last day. This is not every 14 days.
    /// </summary>
    private static IEnumerable<DateOnly> SemimonthlyDates(DateOnly anchor, DateOnly latest)
    {
        var earlyDay = anchor.Day <= 15 ? anchor.Day : anchor.Day - 15;
        var lateDay = earlyDay + 15;
        var limit = new DateOnly(latest.Year, latest.Month, 1);
        for (var month = new DateOnly(anchor.Year, anchor.Month, 1); month <= limit; month = month.AddMonths(1))
        {
            var early = DayInMonth(month, earlyDay);
            var late = DayInMonth(month, lateDay);
            if (early >= anchor && early <= latest)
            {
                yield return early;
            }

            if (late != early && late >= anchor && late <= latest)
            {
                yield return late;
            }
        }
    }

    /// <summary>
    /// A calendar day in that month.
    /// A day number past the last day of the month uses the last day.
    /// </summary>
    private static DateOnly DayInMonth(DateOnly month, int day)
    {
        var last = DateTime.DaysInMonth(month.Year, month.Month);
        return new DateOnly(month.Year, month.Month, Math.Min(day, last));
    }

    #endregion
}
