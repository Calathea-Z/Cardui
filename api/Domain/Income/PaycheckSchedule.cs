using Cardui.Api.Domain;
using Cardui.Api.Models;

namespace Cardui.Api.Domain.Income;

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
    /// Irregular has no dates.
    /// </summary>
    private static IEnumerable<DateOnly> Enumerate(IncomeCadence cadence, DateOnly anchor)
    {
        if (!CadenceDates.TryStep(cadence, out var step))
        {
            return [];
        }

        return CadenceDates.Enumerate(step, anchor, IncomeSourceRules.LatestPaymentDate);
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
