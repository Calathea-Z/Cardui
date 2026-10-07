using Cardui.Api.Domain.Categories;
using Cardui.Api.Models;

namespace Cardui.Api.Domain.Transactions;

public static class TransactionActivityCalculator
{
    /// <summary>
    /// True when a posted transaction counts as income or spending.
    /// Transfers and balance reconciliations are excluded.
    /// </summary>
    public static bool AffectsIncomeOrSpending(TransactionActivityValue transaction)
    {
        return !transaction.Pending
            && !IsTransfer(transaction)
            && !IsBalanceReconciliation(transaction);
    }

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
            if (!AffectsIncomeOrSpending(transaction))
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

    #region Private Methods

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
        return transaction.Provenance == FinancialRecordProvenance.BalanceReconciliation;
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

    #endregion
}
