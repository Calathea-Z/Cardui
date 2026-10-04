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
    /// Keyword categorization for one stored transaction.
    /// A batch should load system category ids once and call FindCategoryId.
    /// </summary>
    Task<Guid?> GetCategoryIdForStoredTransactionAsync(
        string name,
        string? merchantName,
        decimal amount,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads every system category id, keyed by category key, in one query.
    /// </summary>
    Task<IReadOnlyDictionary<string, Guid>> GetSystemCategoryIdsByKeyAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the loaded id for this transaction's keyword category.
    /// Returns null when that category was not seeded.
    /// </summary>
    Guid? FindCategoryId(
        IReadOnlyDictionary<string, Guid> categoryIdsByKey,
        string name,
        string? merchantName,
        decimal amount);
}
