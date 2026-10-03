namespace Cardui.Api.Domain;

public readonly record struct AccountSnapshotBalance(
    Guid AccountId,
    string Type,
    DateOnly Date,
    decimal CurrentBalance,
    DateTimeOffset RecordedAt);
