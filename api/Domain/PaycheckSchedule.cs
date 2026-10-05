using Cardui.Api.Models;

namespace Cardui.Api.Domain;

public static class PaycheckSchedule
{
    /// <summary>
    /// Lists pay dates from the cadence and the next payment, on or after today,
    /// through the end of the month two months after the first of those dates.
    /// Irregular has no dates. Weekly and biweekly step by days, so a month can contain three paychecks.
    /// Semimonthly stays on two days of the month. The list is not a monthly amount placed on one date.
    /// </summary>
    public static IReadOnlyList<DateOnly> UpcomingDates(
        IncomeCadence cadence,
        DateOnly nextPaymentDate,
        DateOnly today)
    {
        if (cadence == IncomeCadence.Irregular)
        {
            return [];
        }

        var onOrAfter = today > nextPaymentDate ? today : nextPaymentDate;
        DateOnly? first = null;
        foreach (var date in Enumerate(cadence, nextPaymentDate))
        {
            if (date >= onOrAfter)
            {
                first = date;
                break;
            }
        }

        if (first is not DateOnly start)
        {
            return [];
        }

        var through = EndOfLaterMonth(start, 2);
        if (through > IncomeSourceRules.LatestPaymentDate)
        {
            through = IncomeSourceRules.LatestPaymentDate;
        }

        var dates = new List<DateOnly>();
        foreach (var date in Enumerate(cadence, nextPaymentDate))
        {
            if (date < start)
            {
                continue;
            }

            if (date > through)
            {
                break;
            }

            dates.Add(date);
        }

        return dates;
    }

    /// <summary>
    /// The average of one payment spread across a year, in dollars per month.
    /// This amount has no date. Irregular has no average.
    /// Biweekly uses 26 payments a year, not two payments in every month.
    /// </summary>
    public static decimal? AverageMonthlyAmount(decimal paymentAmount, IncomeCadence cadence)
    {
        var paymentsPerYear = PaymentsPerYear(cadence);
        if (paymentsPerYear == 0)
        {
            return null;
        }

        return decimal.Round(
            paymentAmount * paymentsPerYear / 12m,
            2,
            MidpointRounding.AwayFromZero);
    }

    #region Private Methods

    /// <summary>
    /// Walks the cadence forward from the next payment, through the latest stored date.
    /// Each step keeps the original anchor so a short month does not move later payments.
    /// </summary>
    private static IEnumerable<DateOnly> Enumerate(IncomeCadence cadence, DateOnly anchor)
    {
        var latest = IncomeSourceRules.LatestPaymentDate;
        switch (cadence)
        {
            case IncomeCadence.Weekly:
                for (var date = anchor; date <= latest; date = date.AddDays(7))
                {
                    yield return date;
                }

                yield break;
            case IncomeCadence.Biweekly:
                for (var date = anchor; date <= latest; date = date.AddDays(14))
                {
                    yield return date;
                }

                yield break;
            case IncomeCadence.Monthly:
                foreach (var date in MonthSteps(anchor, 1, latest))
                {
                    yield return date;
                }

                yield break;
            case IncomeCadence.Quarterly:
                foreach (var date in MonthSteps(anchor, 3, latest))
                {
                    yield return date;
                }

                yield break;
            case IncomeCadence.Yearly:
                foreach (var date in MonthSteps(anchor, 12, latest))
                {
                    yield return date;
                }

                yield break;
            case IncomeCadence.Semimonthly:
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
    /// Adds the same number of months from the anchor for each step.
    /// A day the target month lacks, such as January 31 in February, uses that month's last day.
    /// The following step starts from the anchor again, so March 31 returns after February 28.
    /// </summary>
    private static IEnumerable<DateOnly> MonthSteps(DateOnly anchor, int monthsPerStep, DateOnly latest)
    {
        for (var step = 0; ; step++)
        {
            var date = anchor.AddMonths(step * monthsPerStep);
            if (date > latest)
            {
                yield break;
            }

            yield return date;
        }
    }

    /// <summary>
    /// Two paydays each month, fifteen days apart, using the next payment's day as one of them.
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

    /// <summary>
    /// The last calendar day of the month that many months after the given date.
    /// </summary>
    private static DateOnly EndOfLaterMonth(DateOnly date, int monthsAhead)
    {
        var shifted = new DateOnly(date.Year, date.Month, 1).AddMonths(monthsAhead);
        var last = DateTime.DaysInMonth(shifted.Year, shifted.Month);
        return new DateOnly(shifted.Year, shifted.Month, last);
    }

    /// <summary>
    /// How many payments a full year contains for that cadence.
    /// Irregular is zero because there is no schedule to average.
    /// </summary>
    private static int PaymentsPerYear(IncomeCadence cadence)
    {
        return cadence switch
        {
            IncomeCadence.Weekly => 52,
            IncomeCadence.Biweekly => 26,
            IncomeCadence.Semimonthly => 24,
            IncomeCadence.Monthly => 12,
            IncomeCadence.Quarterly => 4,
            IncomeCadence.Yearly => 1,
            _ => 0
        };
    }

    #endregion
}
