using Cardui.Api.Models;

namespace Cardui.Api.Domain.Debts;

/// <summary>
/// The stored values used to decide a debt's balance.
/// Synced is the account's latest dated balance. It is null when the account has none.
/// SyncFailedOn is that failure in the household time zone, and it is null when the last sync did not fail.
/// </summary>
public sealed record DebtBalanceFacts(
    bool Following,
    bool BalanceOverridden,
    DebtKind Kind,
    string Currency,
    decimal? RecordedBalance,
    DateOnly? RecordedAsOf,
    DebtLinkedBalance? Synced,
    bool AccountActive,
    bool AccountArchived,
    bool BankLinked,
    DateTimeOffset? LastSyncCompletedAt,
    DateTimeOffset? LastSyncFailedAt,
    DateOnly? SyncFailedOn,
    DateOnly Today);
