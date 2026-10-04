using Cardui.Api.Models;

namespace Cardui.Api.Domain;

/// <summary>
/// One transaction reduced to the fields income and spending need.
/// </summary>
public sealed record TransactionActivityValue(
    decimal Amount,
    bool Pending,
    Guid? CategoryId,
    string CategoryName,
    string? CategoryColor,
    string? CategoryKey,
    string? GroupKey,
    string? Provenance = null);

/// <summary>
/// Spending total for one category in a date range.
/// </summary>
public sealed record TransactionActivityCategoryTotal(
    Guid? CategoryId,
    string CategoryName,
    string? CategoryColor,
    decimal Amount);

/// <summary>
/// Income, spending, and spending by category for a set of transactions.
/// </summary>
public sealed record TransactionActivityTotals(
    decimal Income,
    decimal Spending,
    IReadOnlyList<TransactionActivityCategoryTotal> SpendingByCategory);

public static class TransactionActivityCalculator
{
    /// <summary>
    /// Totals income and spending. Pending transactions, transfers, and
    /// balance reconciliations are excluded. Income is stored as a negative
    /// amount and returned as a positive total.
    /// </summary>
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

    /// <summary>
    /// True when the category or its group is Transfers.
    /// </summary>
    private static bool IsTransfer(TransactionActivityValue transaction)
    {
        return HasKey(transaction.GroupKey, SystemGroupKeys.Transfers)
            || HasKey(transaction.CategoryKey, SystemCategoryKeys.Transfers);
    }

    /// <summary>
    /// True for a statement-match adjustment, which is not income or spending.
    /// </summary>
    private static bool IsBalanceReconciliation(TransactionActivityValue transaction)
    {
        return string.Equals(
            transaction.Provenance,
            FinancialRecordProvenance.BalanceReconciliation,
            StringComparison.Ordinal);
    }

    /// <summary>
    /// True when the category or its group is Income.
    /// </summary>
    private static bool IsIncome(TransactionActivityValue transaction)
    {
        return HasKey(transaction.GroupKey, SystemGroupKeys.Income)
            || HasKey(transaction.CategoryKey, SystemCategoryKeys.Income);
    }

    /// <summary>
    /// Compares a category or group key without regard to case.
    /// </summary>
    private static bool HasKey(string? actual, string expected)
    {
        return string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Groups spending by category id, name, and color.
    /// </summary>
    private sealed record CategoryBucket(
        Guid? CategoryId,
        string CategoryName,
        string? CategoryColor);
}
