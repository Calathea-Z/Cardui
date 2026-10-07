using Cardui.Api.Models;

namespace Cardui.Api.Domain.Debts;

/// <summary>
/// The values used to decide a followed credit limit.
/// SyncedLimit is the account's limit. SyncedAsOf is the latest snapshot date, because that sync wrote the limit.
/// </summary>
public sealed record DebtCreditLimitFacts(
    bool Following,
    bool Overridden,
    DebtKind Kind,
    decimal? RecordedLimit,
    decimal? SyncedLimit,
    DateOnly? SyncedAsOf);
