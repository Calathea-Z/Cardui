using Cardui.Api.Domain.Accounts;
using Cardui.Api.Models;

namespace Cardui.Api.Domain.Debts;

/// <summary>
/// Decides the credit limit a followed revolving debt uses.
/// The debt's own column is not changed here. A loan, or a card with no usable limit, keeps the person's value.
/// </summary>
public static class DebtFollowedCreditLimit
{
    /// <summary>
    /// Resolves the credit limit in use from values already stored.
    /// An installment debt does not follow a limit. A missing, zero, negative, or too-large limit is not usable.
    /// </summary>
    public static DebtCreditLimitResolution Resolve(DebtCreditLimitFacts facts)
    {
        if (!facts.Following || facts.Kind != DebtKind.Revolving)
        {
            return Manual(facts.RecordedLimit);
        }

        var synced = Usable(facts.SyncedLimit);
        if (facts.Overridden)
        {
            return new DebtCreditLimitResolution(
                facts.RecordedLimit,
                DebtFieldSource.Override,
                synced,
                synced is null ? null : facts.SyncedAsOf);
        }

        if (synced is not decimal amount)
        {
            return Manual(facts.RecordedLimit);
        }

        return new DebtCreditLimitResolution(
            amount,
            DebtFieldSource.Synced,
            amount,
            facts.SyncedAsOf);
    }

    /// <summary>
    /// True when the person's limit is a different amount from the one following would use.
    /// A missing limit on either side is not a difference. There is nothing to keep or to replace.
    /// </summary>
    public static bool RecordedDiffers(decimal? recorded, decimal? followed)
    {
        if (recorded is not decimal owned || followed is not decimal connected)
        {
            return false;
        }

        return AccountLedger.Round(owned) != AccountLedger.Round(connected);
    }

    /// <summary>
    /// The rounded limit a debt can follow, or null when the connection did not provide one.
    /// Zero and a negative amount are not a credit limit.
    /// </summary>
    public static decimal? Usable(decimal? limit)
    {
        if (limit is not decimal amount)
        {
            return null;
        }

        var rounded = AccountLedger.Round(amount);
        if (rounded <= 0 || rounded > DebtRules.MaxAmount)
        {
            return null;
        }

        return rounded;
    }

    #region Private Methods

    /// <summary>
    /// Uses the debt's own limit, with no synced figure beside it.
    /// </summary>
    private static DebtCreditLimitResolution Manual(decimal? recorded)
    {
        return new DebtCreditLimitResolution(recorded, DebtFieldSource.Manual, null, null);
    }

    #endregion
}
