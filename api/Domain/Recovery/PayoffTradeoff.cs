using Cardui.Api.Domain.Debts;

namespace Cardui.Api.Domain.Recovery;

public static class PayoffTradeoff
{
    /// <summary>
    /// The candidate's interest minus avalanche.
    /// A positive result costs more than paying the highest rate first.
    /// </summary>
    public static decimal InterestDifference(PayoffOrder candidate, PayoffOrder baseline)
    {
        return candidate.TotalInterest - baseline.TotalInterest;
    }

    /// <summary>
    /// Net cash from minimums that end sooner, minus minimums that end later.
    /// Each side is the monthly minimum times the months between the two orders.
    /// A debt that never pays off inside 600 months is counted as month 600.
    /// </summary>
    public static decimal NetMinimumCash(PayoffOrder candidate, PayoffOrder baseline)
    {
        return Cash(candidate, baseline, paidOff: true);
    }

    /// <summary>
    /// Cash from bringing high utilization under 90 percent sooner.
    /// Only a revolving debt that started at or above that line is counted, as its minimum times the months saved.
    /// A debt that never crosses inside 600 months is counted as month 600.
    /// </summary>
    public static decimal UtilizationCash(PayoffOrder candidate, PayoffOrder baseline)
    {
        return Cash(candidate, baseline, paidOff: false);
    }

    /// <summary>
    /// How many months the candidate count is ahead of the baseline count.
    /// A missing count is the 600 month cap. The result is negative when the candidate is later.
    /// </summary>
    public static int MonthsApart(int? baseline, int? candidate)
    {
        var later = baseline ?? DebtRules.MaxRemainingTermMonths;
        var sooner = candidate ?? DebtRules.MaxRemainingTermMonths;
        return later - sooner;
    }

    #region Private Methods

    /// <summary>
    /// Adds each counted debt's minimum times how many months the candidate beats avalanche.
    /// Paid-off months are used for a minimum. Months under the limit notice are used for utilization.
    /// </summary>
    private static decimal Cash(PayoffOrder candidate, PayoffOrder baseline, bool paidOff)
    {
        decimal cash = 0;
        foreach (var debt in candidate.Debts)
        {
            if (debt.Minimum is not decimal minimum)
            {
                continue;
            }

            if (!paidOff
                && (debt.Utilization is not decimal utilization
                    || utilization < DebtSummary.UtilizationLimitNotice))
            {
                continue;
            }

            var other = baseline.Debts.First(item => item.DebtId == debt.DebtId);
            var months = paidOff
                ? MonthsApart(other.PaymentsUntilPaidOff, debt.PaymentsUntilPaidOff)
                : MonthsApart(other.PaymentsUntilUnderLimit, debt.PaymentsUntilUnderLimit);
            cash += months * minimum;
        }

        return cash;
    }

    #endregion
}
