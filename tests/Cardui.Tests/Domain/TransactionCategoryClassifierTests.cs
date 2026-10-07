using Cardui.Api.Domain.Categories;
using Cardui.Api.Domain.Transactions;
using Xunit;

namespace Cardui.Tests.Domain;

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

    [Theory]
    [InlineData("Trader Joe's", SystemCategoryKeys.FoodDining)]
    [InlineData("Shell station", SystemCategoryKeys.AutoTransport)]
    [InlineData("Netflix.com", SystemCategoryKeys.TravelLifestyle)]
    [InlineData("City electric bill", SystemCategoryKeys.BillsUtilities)]
    public void GetCategoryKey_MatchesTheFirstKeywordGroup(string description, string expectedKey)
    {
        var categoryKey = TransactionCategoryClassifier.GetCategoryKey(
            merchantName: null,
            description,
            amount: 20m);

        Assert.Equal(expectedKey, categoryKey);
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
