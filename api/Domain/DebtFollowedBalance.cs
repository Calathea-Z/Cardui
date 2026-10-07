using Cardui.Api.Models;

namespace Cardui.Api.Domain;

/// <summary>
/// Decides the balance a followed debt uses.
/// The debt's own columns are not changed here. A sync does not write them.
/// </summary>
public static class DebtFollowedBalance
{
    /// <summary>
    /// How many days a snapshot may age before a followed balance is stale.
    /// The daily sync can miss one run without marking the debt stale.
    /// </summary>
    public const int StaleAfterDays = 2;

    /// <summary>
    /// Resolves the balance in use from values already stored.
    /// A debt that is not following keeps its own balance. A negative card balance counts as zero.
    /// </summary>
    public static DebtBalanceResolution Resolve(DebtBalanceFacts facts)
    {
        if (!facts.Following)
        {
            return Recorded(facts, DebtFieldSource.Manual, null);
        }

        var synced = ReadSynced(facts.Synced);
        var block = Classify(facts, synced);
        var credit = Credit(facts, block, synced);
        if (credit is not null)
        {
            block = DebtAccountBalanceBlock.None;
        }

        var freshness = Freshness(facts, block);
        var failedOn = freshness == DebtLinkFreshness.SyncFailing ? facts.SyncFailedOn : null;
        if (facts.BalanceOverridden)
        {
            return Recorded(
                facts,
                DebtFieldSource.Override,
                synced,
                block,
                freshness,
                failedOn,
                credit);
        }

        if (credit is not null && synced is { } creditBalance)
        {
            return new DebtBalanceResolution(
                0m,
                creditBalance.AsOf,
                DebtFieldSource.Synced,
                creditBalance.Amount,
                creditBalance.AsOf,
                DebtAccountBalanceBlock.None,
                credit,
                freshness,
                failedOn);
        }

        if (block != DebtAccountBalanceBlock.None || synced is not { } followed)
        {
            return Recorded(facts, DebtFieldSource.Manual, synced, block, freshness, failedOn);
        }

        return new DebtBalanceResolution(
            followed.Amount,
            followed.AsOf,
            DebtFieldSource.Synced,
            followed.Amount,
            followed.AsOf,
            DebtAccountBalanceBlock.None,
            null,
            freshness,
            failedOn);
    }

    /// <summary>
    /// True when the person's balance is a different amount from the one following would use.
    /// A missing balance on either side is not a difference. There is nothing to keep or to replace.
    /// </summary>
    public static bool RecordedDiffers(decimal? recorded, decimal? followed)
    {
        if (recorded is not decimal owned || followed is not decimal connected)
        {
            return false;
        }

        return AccountLedger.Round(owned) != AccountLedger.Round(connected);
    }

    #region Private Methods

    /// <summary>
    /// Uses the debt's own balance. The synced amount stays beside it when the connection has one.
    /// </summary>
    private static DebtBalanceResolution Recorded(
        DebtBalanceFacts facts,
        DebtFieldSource source,
        (decimal Amount, DateOnly AsOf)? synced,
        DebtAccountBalanceBlock block = DebtAccountBalanceBlock.None,
        DebtLinkFreshness? freshness = null,
        DateOnly? failedOn = null,
        decimal? credit = null)
    {
        return new DebtBalanceResolution(
            facts.RecordedBalance,
            facts.RecordedAsOf,
            source,
            synced is { } amount ? amount.Amount : null,
            synced is { } dated ? dated.AsOf : null,
            block,
            credit,
            freshness,
            failedOn);
    }

    /// <summary>
    /// The rounded snapshot, or null when the account has no dated balance.
    /// </summary>
    private static (decimal Amount, DateOnly AsOf)? ReadSynced(DebtLinkedBalance? synced)
    {
        if (synced is not DebtLinkedBalance account || account.AsOf is not DateOnly asOf)
        {
            return null;
        }

        return (AccountLedger.Round(account.Balance), asOf);
    }

    /// <summary>
    /// Why the snapshot cannot be used. A negative revolving balance is a credit when the
    /// rest of the snapshot could be stored. A different currency stays blocked.
    /// </summary>
    private static DebtAccountBalanceBlock Classify(
        DebtBalanceFacts facts,
        (decimal Amount, DateOnly AsOf)? synced)
    {
        if (facts.Synced is not DebtLinkedBalance account || synced is null)
        {
            return DebtAccountBalanceBlock.DateUnknown;
        }

        var block = DebtSummary.ClassifyLinkedBalance(facts.Currency, account);
        if (block != DebtAccountBalanceBlock.NegativeBalance || facts.Kind != DebtKind.Revolving)
        {
            return block;
        }

        var positive = account with { Balance = Math.Abs(account.Balance) };
        return DebtSummary.ClassifyLinkedBalance(facts.Currency, positive);
    }

    /// <summary>
    /// The positive credit on a followed revolving card, or null when the balance is not a credit.
    /// The negative amount is not what the debt uses.
    /// </summary>
    private static decimal? Credit(
        DebtBalanceFacts facts,
        DebtAccountBalanceBlock block,
        (decimal Amount, DateOnly AsOf)? synced)
    {
        if (facts.Kind != DebtKind.Revolving
            || block != DebtAccountBalanceBlock.None
            || synced is not { Amount: < 0 } amount)
        {
            return null;
        }

        return AccountLedger.Round(Math.Abs(amount.Amount));
    }

    /// <summary>
    /// The connection state for a followed debt.
    /// A missing account wins over a removed bank link. A failed sync wins over an old snapshot.
    /// A blocked balance is stale even when the snapshot date is recent.
    /// </summary>
    private static DebtLinkFreshness Freshness(DebtBalanceFacts facts, DebtAccountBalanceBlock block)
    {
        if (!facts.AccountActive || facts.AccountArchived)
        {
            return DebtLinkFreshness.AccountMissing;
        }

        if (!facts.BankLinked)
        {
            return DebtLinkFreshness.Disconnected;
        }

        if (IsSyncFailing(facts.LastSyncCompletedAt, facts.LastSyncFailedAt))
        {
            return DebtLinkFreshness.SyncFailing;
        }

        if (block != DebtAccountBalanceBlock.None || IsStale(facts.Synced?.AsOf, facts.Today))
        {
            return DebtLinkFreshness.Stale;
        }

        return DebtLinkFreshness.Current;
    }

    /// <summary>
    /// True when the latest sync failed after the last success, or failed without one.
    /// </summary>
    private static bool IsSyncFailing(
        DateTimeOffset? completedAt,
        DateTimeOffset? failedAt)
    {
        if (failedAt is not DateTimeOffset failed)
        {
            return false;
        }

        return completedAt is not DateTimeOffset completed || failed > completed;
    }

    /// <summary>
    /// True when the snapshot is missing or older than the stale window.
    /// A snapshot from today or either of the two days before it is still current.
    /// </summary>
    private static bool IsStale(DateOnly? asOf, DateOnly today)
    {
        if (asOf is not DateOnly date)
        {
            return true;
        }

        return today.DayNumber - date.DayNumber > StaleAfterDays;
    }

    #endregion
}
