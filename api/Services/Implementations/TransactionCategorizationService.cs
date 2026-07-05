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
        var categoryName = GetCategoryName(transaction);

        return await _dbContext.Categories
            .AsNoTracking()
            .Where(x => x.Name == categoryName)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync();
    }

    private static string GetCategoryName(PlaidTransaction transaction)
    {
        if (transaction.Amount < 0) return "Income";

        var text = string.Join(" ", new[]
        {
            transaction.MerchantName,
            transaction.OriginalDescription
        }.Where(s => !string.IsNullOrWhiteSpace(s))).ToLowerInvariant();

        if (ContainsAny(text, "transfer", "payment", "ach", "zelle", "venmo", "cash app")) return "Transfers";

        if (ContainsAny(text, "whole foods", "trader joe", "kroger", "safeway", "grocery", "market"))
            return "Groceries";

        if (ContainsAny(text, "restaurant", "cafe", "coffee", "starbucks", "doordash", "uber eats", "chipotle"))
            return "Dining";

        if (ContainsAny(text, "shell", "chevron", "exxon", "gas", "uber", "lyft", "parking")) return "Transport";

        if (ContainsAny(text, "netflix", "spotify", "hulu", "disney", "amc", "cinema", "ticket"))
            return "Entertainment";

        if (ContainsAny(text, "target", "amazon", "walmart", "costco", "best buy")) return "Shopping";

        if (ContainsAny(text, "electric", "utility", "internet", "phone", "insurance", "rent", "mortgage"))
            return "Bills";

        return "Uncategorized";
    }

    private static bool ContainsAny(string text, params string[] keywords)
    {
        return keywords.Any(text.Contains);
    }
}