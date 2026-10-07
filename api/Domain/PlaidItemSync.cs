namespace Cardui.Api.Domain;

/// <summary>
/// Decides whether a Plaid item can start a sync.
/// A start holds the item until a later completion or failure, or until the
/// lease passes. An unfinished start older than the lease was interrupted.
/// </summary>
public static class PlaidItemSync
{
    /// <summary>
    /// How long an unfinished start keeps other syncs from beginning.
    /// One item's account and transaction sync finishes well inside this.
    /// A dead process does not block the next sync for the rest of the day.
    /// </summary>
    public static readonly TimeSpan Lease = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Stored when a sync is cancelled or a start is abandoned past the lease.
    /// The previous success time is left in place.
    /// </summary>
    public const string InterruptedMessage = "Sync was interrupted.";

    /// <summary>
    /// True when this start is still the newest sync event and the lease
    /// has not passed. Another sync must not start.
    /// </summary>
    public static bool HoldsItem(
        DateTimeOffset? startedAt,
        DateTimeOffset? completedAt,
        DateTimeOffset? failedAt,
        DateTimeOffset now)
    {
        if (startedAt is not DateTimeOffset started || IsClosed(started, completedAt, failedAt))
        {
            return false;
        }

        return now - started < Lease;
    }

    /// <summary>
    /// True when a start never completed or failed and the lease has passed.
    /// The next sync records that interruption and may then start.
    /// </summary>
    public static bool IsInterrupted(
        DateTimeOffset? startedAt,
        DateTimeOffset? completedAt,
        DateTimeOffset? failedAt,
        DateTimeOffset now)
    {
        if (startedAt is not DateTimeOffset started || IsClosed(started, completedAt, failedAt))
        {
            return false;
        }

        return now - started >= Lease;
    }

    /// <summary>
    /// A finish time that is not earlier than the start, so a fast sync
    /// still closes the hold when the clock has not moved past the start.
    /// </summary>
    public static DateTimeOffset FinishTime(DateTimeOffset? startedAt, DateTimeOffset now)
    {
        return startedAt is DateTimeOffset started && started > now ? started : now;
    }

    #region Private Methods

    /// <summary>
    /// True when a completion or failure is at or after the start.
    /// That start no longer holds the item.
    /// </summary>
    private static bool IsClosed(
        DateTimeOffset started,
        DateTimeOffset? completedAt,
        DateTimeOffset? failedAt)
    {
        var finishedAt = Later(completedAt, failedAt);
        return finishedAt is DateTimeOffset finished && started <= finished;
    }

    /// <summary>
    /// The later of the two timestamps, or the one that is present.
    /// </summary>
    private static DateTimeOffset? Later(DateTimeOffset? left, DateTimeOffset? right)
    {
        if (left is null)
        {
            return right;
        }

        if (right is null)
        {
            return left;
        }

        return left > right ? left : right;
    }

    #endregion
}
