using Cardui.Api.Domain;
using Cardui.Api.Services;
using Xunit;

namespace Cardui.Tests.Services;

public class TransactionCategoryClassifierTests
{
    [Theory]
    [InlineData("Employer payroll")]
    [InlineData("ACH direct deposit")]
    [InlineData("Monthly pension")]
    public void GetCategoryKey_ClassifiesRecognizableIncomingIncome(string description)
    {
        var categoryKey = TransactionCategoryClassifier.GetCategoryKey(
            merchantName: null,
            description,
            amount: -1_000m);

        Assert.Equal(SystemCategoryKeys.Income, categoryKey);
    }

    [Fact]
    public void GetCategoryKey_KeepsMerchantRefundInExpenseCategory()
    {
        var categoryKey = TransactionCategoryClassifier.GetCategoryKey(
            merchantName: "Amazon",
            description: "Refund",
            amount: -25m);

        Assert.Equal(SystemCategoryKeys.Shopping, categoryKey);
    }

    [Fact]
    public void GetCategoryKey_DoesNotInferIncomeFromNegativeAmountAlone()
    {
        var categoryKey = TransactionCategoryClassifier.GetCategoryKey(
            merchantName: null,
            description: "Card credit",
            amount: -25m);

        Assert.Equal(SystemCategoryKeys.Other, categoryKey);
    }

    [Fact]
    public void GetCategoryKey_PrioritizesBankTransferOverIncomeText()
    {
        var categoryKey = TransactionCategoryClassifier.GetCategoryKey(
            merchantName: null,
            description: "Online transfer from payroll checking",
            amount: -1_000m);

        Assert.Equal(SystemCategoryKeys.Transfers, categoryKey);
    }
}
