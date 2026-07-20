using Cardui.Api.Data;
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
        PlaidTransaction transaction)
    {
        var categoryKey = GetCategoryKey(
            transaction.MerchantName,
            transaction.OriginalDescription,
            transaction.Amount);

        return ResolveCategoryIdAsync(categoryKey);
    }

    public Task<Guid?> GetCategoryIdForStoredTransactionAsync(
        string name,
        string? merchantName,
        decimal amount)
    {
        var categoryKey = GetCategoryKey(merchantName, name, amount);
        return ResolveCategoryIdAsync(categoryKey);
    }

    private async Task<Guid?> ResolveCategoryIdAsync(string categoryKey)
    {
        return await _dbContext.Categories
            .AsNoTracking()
            .Where(x => x.Key == categoryKey)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync();
    }

    #region Private Methods

    private static string GetCategoryKey(
        string? merchantName,
        string? description,
        decimal? amount)
    {
        var text = TransferTextClassifier.BuildText(merchantName, description);

        // Bank "ONLINE TRANSFER FROM/TO …" rows are transfers even without a
        // paired opposite leg. Check before income so credits aren't treated
        // as positive spending power.
        if (TransferTextClassifier.LooksLikeBankTransfer(merchantName, description))
        {
            return "transfers";
        }

        // Venmo/Zelle/etc. stay income or spend until (or unless) a matching
        // opposite leg exists on another account the user owns.
        if (amount < 0)
        {
            return "income";
        }

        if (ContainsAny(text, "whole foods", "trader joe", "kroger", "safeway", "grocery", "market"))
        {
            return "groceries";
        }

        if (ContainsAny(text, "restaurant", "cafe", "coffee", "starbucks", "doordash", "uber eats", "chipotle"))
        {
            return "dining";
        }

        if (ContainsAny(text, "shell", "chevron", "exxon", "gas", "uber", "lyft", "parking"))
        {
            return "transport";
        }

        if (ContainsAny(text, "netflix", "spotify", "hulu", "disney", "amc", "cinema", "ticket"))
        {
            return "entertainment";
        }

        if (ContainsAny(text, "target", "amazon", "walmart", "costco", "best buy"))
        {
            return "shopping";
        }

        if (ContainsAny(text, "electric", "utility", "internet", "phone", "insurance", "rent", "mortgage", "t-mobile", "tmobile", "verizon", "at&t", "att "))
        {
            return "bills";
        }

        return "uncategorized";
    }

    private static bool ContainsAny(string text, params string[] keywords)
    {
        return keywords.Any(text.Contains);
    }

    #endregion
}
