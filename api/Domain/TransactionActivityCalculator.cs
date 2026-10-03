using Cardui.Api.Models;

namespace Cardui.Api.Domain;

public sealed record TransactionActivityValue(
    decimal Amount,
    bool Pending,
    Guid? CategoryId,
    string CategoryName,
    string? CategoryColor,
    string? CategoryKey,
    string? GroupKey,
    string? Provenance = null);

public sealed record TransactionActivityCategoryTotal(
    Guid? CategoryId,
    string CategoryName,
    string? CategoryColor,
    decimal Amount);

public sealed record TransactionActivityTotals(
    decimal Income,
    decimal Spending,
    IReadOnlyList<TransactionActivityCategoryTotal> SpendingByCategory);

public static class TransactionActivityCalculator
{
    public static TransactionActivityTotals Calculate(
        IEnumerable<TransactionActivityValue> transactions)
    {
        decimal netIncome = 0;
        var spendingByCategory = new Dictionary<CategoryBucket, decimal>();

        foreach (var transaction in transactions)
        {
            if (transaction.Pending
                || IsTransfer(transaction)
                || IsBalanceReconciliation(transaction))
            {
                continue;
            }

            if (IsIncome(transaction))
            {
                netIncome -= transaction.Amount;
                continue;
            }

            var bucket = new CategoryBucket(
                transaction.CategoryId,
                transaction.CategoryName,
                transaction.CategoryColor);

            spendingByCategory.TryGetValue(bucket, out var currentAmount);
            spendingByCategory[bucket] = currentAmount + transaction.Amount;
        }

        var categoryTotals = spendingByCategory
            .Where(x => x.Value > 0)
            .Select(x => new TransactionActivityCategoryTotal(
                x.Key.CategoryId,
                x.Key.CategoryName,
                x.Key.CategoryColor,
                x.Value))
            .OrderByDescending(x => x.Amount)
            .ThenBy(x => x.CategoryName)
            .ToList();

        return new TransactionActivityTotals(
            Income: Math.Max(0, netIncome),
            Spending: categoryTotals.Sum(x => x.Amount),
            SpendingByCategory: categoryTotals);
    }

    private static bool IsTransfer(TransactionActivityValue transaction)
    {
        return HasKey(transaction.GroupKey, SystemGroupKeys.Transfers)
            || HasKey(transaction.CategoryKey, SystemCategoryKeys.Transfers);
    }

    private static bool IsBalanceReconciliation(TransactionActivityValue transaction)
    {
        return string.Equals(
            transaction.Provenance,
            FinancialRecordProvenance.BalanceReconciliation,
            StringComparison.Ordinal);
    }

    private static bool IsIncome(TransactionActivityValue transaction)
    {
        return HasKey(transaction.GroupKey, SystemGroupKeys.Income)
            || HasKey(transaction.CategoryKey, SystemCategoryKeys.Income);
    }

    private static bool HasKey(string? actual, string expected)
    {
        return string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);
    }

    private sealed record CategoryBucket(
        Guid? CategoryId,
        string CategoryName,
        string? CategoryColor);
}
