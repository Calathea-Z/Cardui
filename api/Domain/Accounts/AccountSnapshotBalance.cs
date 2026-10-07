namespace Cardui.Api.Domain.Accounts;

/// <summary>
/// One stored balance used to build history. RecordedAt orders snapshots on the same day.
/// </summary>
public readonly record struct AccountSnapshotBalance(
    Guid AccountId,
    string Type,
    DateOnly Date,
    decimal CurrentBalance,
    DateTimeOffset RecordedAt);
