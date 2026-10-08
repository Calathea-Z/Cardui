using Cardui.Api.Domain.Accounts;

namespace Cardui.Api.Domain.Debts;

public static class DebtInterest
{
    /// <summary>
    /// One month of simple interest on a balance.
    /// The amount is the balance times the annual rate, divided by 12, rounded to cents.
    /// A zero rate is zero interest. This is not a daily balance and not the total interest left to pay.
    /// </summary>
    public static decimal ForMonth(decimal balance, decimal ratePercent)
    {
        return AccountLedger.Round(balance * ratePercent / 100m / 12m);
    }
}
