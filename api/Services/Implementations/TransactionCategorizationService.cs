using Cardui.Api.Data;
using Cardui.Api.Domain;
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
        var text = TransferTextClassifier.BuildText(merchantName, description);

        if (TransferTextClassifier.LooksLikeBankTransfer(merchantName, description))
        {
            return SystemCategoryKeys.Transfers;
        }

        if (amount < 0)
        {
            return SystemCategoryKeys.Income;
        }

        if (ContainsAny(text, "whole foods", "trader joe", "kroger", "safeway", "grocery", "market"))
        {
            return SystemCategoryKeys.FoodDining;
        }

        if (ContainsAny(text, "restaurant", "cafe", "coffee", "starbucks", "doordash", "uber eats", "chipotle"))
        {
            return SystemCategoryKeys.FoodDining;
        }

        if (ContainsAny(text, "shell", "chevron", "exxon", "gas", "uber", "lyft", "parking"))
        {
            return SystemCategoryKeys.AutoTransport;
        }

        if (ContainsAny(text, "netflix", "spotify", "hulu", "disney", "amc", "cinema", "ticket"))
        {
            return SystemCategoryKeys.TravelLifestyle;
        }

        if (ContainsAny(text, "target", "amazon", "walmart", "costco", "best buy"))
        {
            return SystemCategoryKeys.Shopping;
        }

        if (ContainsAny(text, "electric", "utility", "internet", "phone", "insurance", "rent", "mortgage", "t-mobile", "tmobile", "verizon", "at&t", "att "))
        {
            return SystemCategoryKeys.BillsUtilities;
        }

        return SystemCategoryKeys.Other;
    }

    private static bool ContainsAny(string text, params string[] keywords)
    {
        return keywords.Any(text.Contains);
    }
}
