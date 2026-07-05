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

    public async Task<Guid?> GetCategoryIdForPlaidTransactionAsync(
        PlaidTransaction transaction)
    {
        var categoryKey = GetCategoryKey(transaction);

        return await _dbContext.Categories
            .AsNoTracking()
            .Where(x => x.Key == categoryKey)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync();
    }

    #region Private Methods

    private static string GetCategoryKey(PlaidTransaction transaction)
    {
        if (transaction.Amount < 0) return "income";

        var text = string.Join(" ", new[]
        {
            transaction.MerchantName,
            transaction.OriginalDescription
        }.Where(s => !string.IsNullOrWhiteSpace(s))).ToLowerInvariant();

        if (ContainsAny(text, "transfer", "payment", "ach", "zelle", "venmo", "cash app")) return "transfers";

        if (ContainsAny(text, "whole foods", "trader joe", "kroger", "safeway", "grocery", "market"))
            return "groceries";

        if (ContainsAny(text, "restaurant", "cafe", "coffee", "starbucks", "doordash", "uber eats", "chipotle"))
            return "dining";

        if (ContainsAny(text, "shell", "chevron", "exxon", "gas", "uber", "lyft", "parking")) return "transport";

        if (ContainsAny(text, "netflix", "spotify", "hulu", "disney", "amc", "cinema", "ticket"))
            return "entertainment";

        if (ContainsAny(text, "target", "amazon", "walmart", "costco", "best buy")) return "shopping";

        if (ContainsAny(text, "electric", "utility", "internet", "phone", "insurance", "rent", "mortgage"))
            return "bills";

        return "uncategorized";
    }

    private static bool ContainsAny(string text, params string[] keywords)
    {
        return keywords.Any(text.Contains);
    }

    #endregion
}