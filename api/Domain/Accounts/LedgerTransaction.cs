namespace Cardui.Api.Domain.Accounts;

/// <summary>
/// A posted, pending, or archived transaction amount used by the manual ledger.
/// </summary>
public readonly record struct LedgerTransaction(
    DateOnly Date,
    decimal Amount,
    bool Pending,
    bool Archived);
