using Cardui.Api.Domain.Accounts;

namespace Cardui.Api.Domain.Living;

public static class LivingContributionRules
{
    private const decimal MaxAmount = 100_000_000m;

    /// <summary>
    /// Checks a monthly contribution.
    /// Null means the amount is not set. Zero is a known zero. A negative amount is rejected.
    /// </summary>
    public static bool TryNormalize(decimal? amount, out decimal? stored, out string error)
    {
        if (amount is null)
        {
            stored = null;
            error = "";
            return true;
        }

        if (amount < 0)
        {
            stored = null;
            error = "Enter an amount of zero or more.";
            return false;
        }

        if (amount > MaxAmount)
        {
            stored = null;
            error = "Enter a smaller amount.";
            return false;
        }

        stored = AccountLedger.Round(amount.Value);
        error = "";
        return true;
    }
}
