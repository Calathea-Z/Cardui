using Cardui.Api.Domain.Accounts;
using Cardui.Api.Domain.Recovery;

namespace Cardui.Api.Domain.Savings;

public static class EverydaySpending
{
    public const int HorizonMonths = 18;

    /// <summary>
    /// The days this month's spending and later months leave cash.
    /// A ready day still ahead counts the full monthly amount. A ready day already past counts only what is left this month, on today.
    /// Later months count the full amount on that day. A day past the end of a short month uses the month's last day.
    /// </summary>
    public static IReadOnlyList<CashFlowEvent> Schedule(
        DateOnly today,
        int readyDay,
        decimal monthlyAmount,
        decimal availableNow,
        Guid sourceId,
        string name,
        string currency)
    {
        var monthly = AccountLedger.Round(monthlyAmount);
        var events = new List<CashFlowEvent>();
        if (monthly <= 0)
        {
            return events;
        }

        var readyThisMonth = OnDay(today.Year, today.Month, readyDay);
        if (today <= readyThisMonth)
        {
            events.Add(Event(readyThisMonth, monthly, sourceId, name, currency));
        }
        else
        {
            var left = AccountLedger.Round(availableNow);
            if (left > 0)
            {
                events.Add(Event(today, left, sourceId, name, currency));
            }
        }

        var end = today.AddMonths(HorizonMonths);
        var cursor = readyThisMonth.AddMonths(1);
        while (cursor <= end)
        {
            var ready = OnDay(cursor.Year, cursor.Month, readyDay);
            if (ready > end)
            {
                break;
            }

            events.Add(Event(ready, monthly, sourceId, name, currency));
            cursor = cursor.AddMonths(1);
        }

        return events;
    }

    /// <summary>
    /// The ready day in one month.
    /// A 31st in a shorter month is that month's last day.
    /// </summary>
    public static DateOnly OnDay(int year, int month, int readyDay)
    {
        var day = Math.Clamp(readyDay, 1, DateTime.DaysInMonth(year, month));
        return new DateOnly(year, month, day);
    }

    #region Private Methods

    /// <summary>
    /// One everyday-spending amount that leaves cash.
    /// The amount is positive. The kind says it is spending, not a bill and not a reserve.
    /// </summary>
    private static CashFlowEvent Event(
        DateOnly date,
        decimal amount,
        Guid sourceId,
        string name,
        string currency)
    {
        return new CashFlowEvent(
            date,
            CashFlowKind.EverydaySpending,
            sourceId,
            name,
            amount,
            currency);
    }

    #endregion
}
