using Cardui.Api.Domain.Accounts;

namespace Cardui.Api.Domain.Savings;

public static class SavingsAmount
{
    /// <summary>
    /// The amount the plan treats as already set aside.
    /// A followed account with no override uses its balance. A negative balance counts as zero.
    /// Any other case uses the typed amount, and a negative typed amount counts as zero.
    /// </summary>
    public static decimal InUse(
        bool following,
        bool overridden,
        decimal reservedAmount,
        decimal? accountBalance)
    {
        if (following && !overridden)
        {
            if (accountBalance is not decimal balance || balance <= 0)
            {
                return 0;
            }

            return AccountLedger.Round(balance);
        }

        return reservedAmount <= 0 ? 0 : AccountLedger.Round(reservedAmount);
    }
}
