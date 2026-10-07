using Cardui.Api.Domain.Accounts;
using Cardui.Api.Domain.Debts;

namespace Cardui.Api.Domain.Recovery;

public static class DebtPeriodCalculator
{
    /// <summary>
    /// Applies one month of interest and one payment.
    /// The payment is the minimum plus extra, and it is not larger than the balance plus interest.
    /// Extra above that room is not taken. Interest uses the same monthly simple-interest rule as the debt summary.
    /// </summary>
    public static DebtPeriod Calculate(
        DateOnly dueDate,
        decimal balance,
        decimal ratePercent,
        bool rateIsPromotional,
        decimal minimum,
        decimal extra)
    {
        var starting = AccountLedger.Round(balance);
        var interest = DebtInterest.ForMonth(starting, ratePercent);
        var owed = starting + interest;
        var minimumPaid = Cap(minimum, owed);
        var extraPaid = Cap(extra, owed - minimumPaid);
        var payment = minimumPaid + extraPaid;
        return new DebtPeriod(
            dueDate,
            starting,
            interest,
            minimumPaid,
            extraPaid,
            payment,
            payment - interest,
            owed - payment,
            ratePercent,
            rateIsPromotional);
    }

    #region Private Methods

    /// <summary>
    /// Limits an amount to what is still owed.
    /// Zero and a negative amount pay nothing. The result is cents.
    /// </summary>
    private static decimal Cap(decimal amount, decimal limit)
    {
        if (amount <= 0 || limit <= 0)
        {
            return 0;
        }

        var rounded = AccountLedger.Round(amount);
        return rounded > limit ? limit : rounded;
    }

    #endregion
}
