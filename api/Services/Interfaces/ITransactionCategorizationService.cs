using PlaidTransaction = Going.Plaid.Entity.Transaction;

namespace Cardui.Api.Services.Interfaces;

public interface ITransactionCategorizationService
{
    Task<Guid?> GetCategoryIdForPlaidTransactionAsync(PlaidTransaction transaction);

    /// <summary>
    /// Keyword categorization for a stored transaction (used when repairing
    /// false-positive transfer pairs).
    /// </summary>
    Task<Guid?> GetCategoryIdForStoredTransactionAsync(
        string name,
        string? merchantName,
        decimal amount);
}
