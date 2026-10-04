using PlaidTransaction = Going.Plaid.Entity.Transaction;

namespace Cardui.Api.Services.Interfaces;

public interface ITransactionCategorizationService
{
    /// <summary>
    /// Picks a system category for a Plaid transaction from its merchant,
    /// description, and amount. Returns null when that category key is not seeded.
    /// </summary>
    Task<Guid?> GetCategoryIdForPlaidTransactionAsync(
        PlaidTransaction transaction,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Keyword categorization for a stored transaction (used when repairing
    /// false-positive transfer pairs).
    /// </summary>
    Task<Guid?> GetCategoryIdForStoredTransactionAsync(
        string name,
        string? merchantName,
        decimal amount,
        CancellationToken cancellationToken = default);
}
