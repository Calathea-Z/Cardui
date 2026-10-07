using Cardui.Api.Domain.Categories;
using Cardui.Api.Domain.Transactions;
using Cardui.Api.Models;
using Xunit;

namespace Cardui.Tests.Domain;

public class TransactionActivityCalculatorTests
{
    [Fact]
    public void Calculate_AppliesPostedActivityConventions()
    {
        var groceriesId = Guid.NewGuid();
        var uncategorizedId = (Guid?)null;

        var result = TransactionActivityCalculator.Calculate(
        [
            Activity(-3_000m, "paycheck", SystemGroupKeys.Income),
            Activity(200m, "paycheck", SystemGroupKeys.Income),
            Activity(500m, "groceries", SystemGroupKeys.Expenses, groceriesId),
            Activity(-125m, "groceries", SystemGroupKeys.Expenses, groceriesId),
            Activity(80m, "Uncategorized", null, uncategorizedId),
            Activity(-20m, "Uncategorized", null, uncategorizedId),
            Activity(900m, "transfer", SystemGroupKeys.Transfers),
            Activity(-900m, "transfer", SystemGroupKeys.Transfers),
            Activity(75m, "pending purchase", SystemGroupKeys.Expenses, pending: true),
            Activity(-1_000m, "pending paycheck", SystemGroupKeys.Income, pending: true)
        ]);

        Assert.Equal(2_800m, result.Income);
        Assert.Equal(435m, result.Spending);

        Assert.Collection(
            result.SpendingByCategory,
            groceries =>
            {
                Assert.Equal(groceriesId, groceries.CategoryId);
                Assert.Equal(375m, groceries.Amount);
            },
            uncategorized =>
            {
                Assert.Null(uncategorized.CategoryId);
                Assert.Equal(60m, uncategorized.Amount);
            });
    }

    [Fact]
    public void Calculate_DoesNotTreatUncategorizedCreditAsIncome()
    {
        var result = TransactionActivityCalculator.Calculate(
        [
            Activity(-50m, "Uncategorized", null)
        ]);

        Assert.Equal(0m, result.Income);
        Assert.Equal(0m, result.Spending);
        Assert.Empty(result.SpendingByCategory);
    }

    [Fact]
    public void Calculate_FloorsReversedIncomeAndRefundedCategoryAtZero()
    {
        var result = TransactionActivityCalculator.Calculate(
        [
            Activity(-100m, "paycheck", SystemGroupKeys.Income),
            Activity(150m, "paycheck", SystemGroupKeys.Income),
            Activity(50m, "shopping", SystemGroupKeys.Expenses),
            Activity(-75m, "shopping", SystemGroupKeys.Expenses)
        ]);

        Assert.Equal(0m, result.Income);
        Assert.Equal(0m, result.Spending);
        Assert.Empty(result.SpendingByCategory);
    }

    [Fact]
    public void Calculate_RecognizesSystemCategoryKeysWithoutGroupMetadata()
    {
        var result = TransactionActivityCalculator.Calculate(
        [
            Activity(-500m, "income", null, categoryKey: SystemCategoryKeys.Income),
            Activity(500m, "transfer", null, categoryKey: SystemCategoryKeys.Transfers)
        ]);

        Assert.Equal(500m, result.Income);
        Assert.Equal(0m, result.Spending);
    }

    [Fact]
    public void Calculate_IgnoresBalanceReconciliationEvenWhenCategorizedAsIncome()
    {
        var result = TransactionActivityCalculator.Calculate(
        [
            Activity(-80m, "Income", SystemGroupKeys.Income, categoryKey: SystemCategoryKeys.Income)
                with { Provenance = FinancialRecordProvenance.BalanceReconciliation },
            Activity(40m, "Uncategorized", null)
                with { Provenance = FinancialRecordProvenance.BalanceReconciliation },
            Activity(25m, "groceries", SystemGroupKeys.Expenses)
        ]);

        Assert.Equal(0m, result.Income);
        Assert.Equal(25m, result.Spending);
    }

    private static TransactionActivityValue Activity(
        decimal amount,
        string categoryName,
        string? groupKey,
        Guid? categoryId = null,
        bool pending = false,
        string? categoryKey = null)
    {
        return new TransactionActivityValue(
            amount,
            pending,
            categoryId,
            categoryName,
            CategoryColor: null,
            categoryKey,
            groupKey);
    }
}
