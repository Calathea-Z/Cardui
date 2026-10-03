using Cardui.Api.Data;
using Cardui.Api.Services;
using Cardui.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using PlaidTransaction = Going.Plaid.Entity.Transaction;

namespace Cardui.Api.Services.Implementations;

public class TransactionCategorizationService : ITransactionCategorizationService
{
    private readonly CarduiDBContext _dbContext;

    public TransactionCategorizationService(CarduiDBContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Guid?> GetCategoryIdForPlaidTransactionAsync(
        PlaidTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        var categoryKey = GetCategoryKey(
            transaction.MerchantName,
            transaction.OriginalDescription,
            transaction.Amount);

        return ResolveCategoryIdAsync(categoryKey, cancellationToken);
    }

    public Task<Guid?> GetCategoryIdForStoredTransactionAsync(
        string name,
        string? merchantName,
        decimal amount,
        CancellationToken cancellationToken = default)
    {
        var categoryKey = GetCategoryKey(merchantName, name, amount);
        return ResolveCategoryIdAsync(categoryKey, cancellationToken);
    }

    private async Task<Guid?> ResolveCategoryIdAsync(
        string categoryKey,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Categories
            .AsNoTracking()
            .Where(x => x.Key == categoryKey)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static string GetCategoryKey(
        string? merchantName,
        string? description,
        decimal? amount)
    {
        return TransactionCategoryClassifier.GetCategoryKey(
            merchantName,
            description,
            amount);
    }
}
