using Cardui.Api.Domain.Categories;
using Cardui.Api.Models;

namespace Cardui.Api.Domain.Transactions;

public static class TransactionActivityCalculator
{
    /// <summary>
    /// True when a posted transaction counts as income or spending.
    /// Transfers, card payments, and balance reconciliations are excluded.
    /// A card payment moves cash or debt and is not a second copy of the purchase.
    /// </summary>
    public static bool AffectsIncomeOrSpending(TransactionActivityValue transaction)
    {
        if (transaction.Pending || IsBalanceReconciliation(transaction))
        {
            return false;
        }

        var kind = CardActivityRules.Classify(Input(transaction)).Kind;
        if (kind == CardActivityKind.CardPayment)
        {
            return false;
        }

        return IsIncome(transaction)
            || kind is CardActivityKind.Purchase or CardActivityKind.Refund;
    }

    /// <summary>
    /// Totals income and spending. Pending transactions, transfers, card payments, and
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
    /// Copies the activity row into the card-activity input.
    /// </summary>
    private static CardActivityInput Input(TransactionActivityValue transaction)
    {
        return new CardActivityInput(
            transaction.AccountType,
            transaction.Amount,
            transaction.Pending,
            transaction.Provenance,
            transaction.CategoryKey,
            transaction.GroupKey,
            transaction.MerchantName,
            transaction.Name);
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
