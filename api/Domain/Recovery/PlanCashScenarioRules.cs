namespace Cardui.Api.Domain.Recovery;

public static class PlanCashScenarioRules
{
    /// <summary>
    /// The first reason a preview cannot be calculated, or null when the amounts are usable.
    /// A windfall needs both a positive amount and a date inside the forecast. Flexible spending can be tried only after Plan budget has an amount.
    /// </summary>
    public static string? Reject(
        decimal monthlyExtra,
        int incomeLossMonths,
        decimal windfallAmount,
        DateOnly? windfallOn,
        decimal? livingSpendingMonthly,
        decimal? cashFloor,
        decimal storedLivingSpending,
        DateOnly today)
    {
        if (monthlyExtra < 0)
        {
            return "Extra each month cannot be negative.";
        }

        if (incomeLossMonths < 0 || incomeLossMonths > PlanCashScenario.MaxMonths)
        {
            return "Income loss has to be from 0 to 18 months.";
        }

        if (windfallAmount < 0)
        {
            return "A windfall cannot be negative.";
        }

        if (windfallAmount == 0 && windfallOn is not null)
        {
            return "Enter a windfall amount, or clear the date.";
        }

        if (windfallAmount > 0 && windfallOn is null)
        {
            return "Choose the date the windfall arrives.";
        }

        if (windfallOn is DateOnly date && !InForecast(today, date))
        {
            return date < today
                ? "A windfall has to arrive today or later."
                : "A windfall has to fall inside the 18-month forecast.";
        }

        if (livingSpendingMonthly is decimal spending)
        {
            if (spending < 0)
            {
                return "Flexible spending cannot be negative.";
            }

            if (storedLivingSpending <= 0)
            {
                return "Set flexible monthly spending on Plan budget before trying a different amount.";
            }
        }

        if (cashFloor is decimal floor && floor < 0)
        {
            return "Cash to keep cannot be negative.";
        }

        return null;
    }

    #region Private Methods

    /// <summary>
    /// True when the date is today through the last day of the 18-month forecast.
    /// The last day is the day before the same calendar day 18 months later.
    /// </summary>
    private static bool InForecast(DateOnly today, DateOnly date)
    {
        if (date < today)
        {
            return false;
        }

        try
        {
            var end = today.AddMonths(PlanCashScenario.MaxMonths).AddDays(-1);
            return date <= end;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    #endregion
}
