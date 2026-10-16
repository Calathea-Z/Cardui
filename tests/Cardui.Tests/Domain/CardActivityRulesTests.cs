using Cardui.Api.Domain.Accounts;
using Cardui.Api.Domain.Categories;
using Cardui.Api.Domain.Transactions;
using Cardui.Api.Models;
using Xunit;

namespace Cardui.Tests.Domain;

public class CardActivityRulesTests
{
    [Fact]
    public void Classify_CountsACardPurchaseOnceAndLeavesCashUnchanged()
    {
        var effect = CardActivityRules.Classify(Input(
            AccountTypes.Credit,
            40m,
            SystemCategoryKeys.FoodDining,
            SystemGroupKeys.Expenses,
            "Trader Joe"));

        Assert.Equal(CardActivityKind.Purchase, effect.Kind);
        Assert.Equal(40m, effect.SpendingAmount);
        Assert.Equal(0m, effect.CashChange);
        Assert.Equal(40m, effect.DebtChange);
    }

    [Fact]
    public void Classify_CountsACashPurchaseOnceAndReducesCash()
    {
        var effect = CardActivityRules.Classify(Input(
            AccountTypes.Depository,
            40m,
            SystemCategoryKeys.FoodDining,
            SystemGroupKeys.Expenses,
            "Trader Joe"));

        Assert.Equal(CardActivityKind.Purchase, effect.Kind);
        Assert.Equal(40m, effect.SpendingAmount);
        Assert.Equal(-40m, effect.CashChange);
        Assert.Equal(0m, effect.DebtChange);
    }

    [Fact]
    public void Classify_TreatsACardRefundAsSpendingReliefAndLowerDebt()
    {
        var effect = CardActivityRules.Classify(Input(
            AccountTypes.Credit,
            -12m,
            SystemCategoryKeys.Shopping,
            SystemGroupKeys.Expenses,
            "Amazon refund"));

        Assert.Equal(CardActivityKind.Refund, effect.Kind);
        Assert.Equal(-12m, effect.SpendingAmount);
        Assert.Equal(0m, effect.CashChange);
        Assert.Equal(-12m, effect.DebtChange);
    }

    [Fact]
    public void Classify_DoesNotCountThePurchaseAndThePaymentTwice()
    {
        var purchase = CardActivityRules.Classify(Input(
            AccountTypes.Credit,
            40m,
            SystemCategoryKeys.FoodDining,
            SystemGroupKeys.Expenses,
            "Trader Joe"));
        var fromCash = CardActivityRules.Classify(Input(
            AccountTypes.Depository,
            40m,
            SystemCategoryKeys.FoodDining,
            SystemGroupKeys.Expenses,
            "Chase credit crd autopay"));
        var onCard = CardActivityRules.Classify(Input(
            AccountTypes.Credit,
            -40m,
            SystemCategoryKeys.FoodDining,
            SystemGroupKeys.Expenses,
            "Payment thank you"));

        Assert.Equal(40m, purchase.SpendingAmount + fromCash.SpendingAmount + onCard.SpendingAmount);
        Assert.Equal(-40m, purchase.CashChange + fromCash.CashChange + onCard.CashChange);
        Assert.Equal(0m, purchase.DebtChange + fromCash.DebtChange + onCard.DebtChange);
        Assert.Equal(CardActivityKind.CardPayment, fromCash.Kind);
        Assert.Equal(CardActivityKind.CardPayment, onCard.Kind);
    }

    [Fact]
    public void Classify_KeepsAMortgagePaymentAsSpending()
    {
        var effect = CardActivityRules.Classify(Input(
            AccountTypes.Depository,
            400m,
            SystemCategoryKeys.BillsUtilities,
            SystemGroupKeys.Expenses,
            "Mortgage"));

        Assert.Equal(CardActivityKind.Purchase, effect.Kind);
        Assert.Equal(400m, effect.SpendingAmount);
        Assert.Equal(-400m, effect.CashChange);
        Assert.Equal(0m, effect.DebtChange);
    }

    [Fact]
    public void Classify_KeepsAnElectricAutopayAsSpending()
    {
        var effect = CardActivityRules.Classify(Input(
            AccountTypes.Depository,
            90m,
            SystemCategoryKeys.BillsUtilities,
            SystemGroupKeys.Expenses,
            "Autopay electric"));

        Assert.Equal(CardActivityKind.Purchase, effect.Kind);
        Assert.Equal(90m, effect.SpendingAmount);
    }

    [Theory]
    [InlineData("Payment thank you")]
    [InlineData("CAPITAL ONE CRCARDPMT")]
    [InlineData("Citi card online payment")]
    [InlineData("Amex epayment")]
    public void LooksLikeCardPayment_MatchesCardSettlementPhrases(string text)
    {
        Assert.True(CardActivityRules.LooksLikeCardPayment(null, text));
    }

    [Theory]
    [InlineData("Autopay electric")]
    [InlineData("Annual credit card fee")]
    [InlineData("Amazon refund")]
    public void LooksLikeCardPayment_LeavesBillsFeesAndRefundsAlone(string text)
    {
        Assert.False(CardActivityRules.LooksLikeCardPayment(null, text));
    }

    [Fact]
    public void Classify_TreatsARepaymentCreditOnACardAsARefund()
    {
        var effect = CardActivityRules.Classify(Input(
            AccountTypes.Credit,
            -15m,
            SystemCategoryKeys.Other,
            SystemGroupKeys.Expenses,
            "Repayment"));

        Assert.Equal(CardActivityKind.Refund, effect.Kind);
        Assert.Equal(-15m, effect.SpendingAmount);
    }

    [Fact]
    public void Classify_IgnoresPendingPurchasesAndStatementAdjustments()
    {
        var pending = CardActivityRules.Classify(Input(
            AccountTypes.Credit,
            40m,
            SystemCategoryKeys.FoodDining,
            SystemGroupKeys.Expenses,
            "Trader Joe",
            pending: true));
        var adjustment = CardActivityRules.Classify(Input(
            AccountTypes.Depository,
            40m,
            SystemCategoryKeys.FoodDining,
            SystemGroupKeys.Expenses,
            "Statement match",
            provenance: FinancialRecordProvenance.BalanceReconciliation));

        Assert.Equal(CardActivityKind.None, pending.Kind);
        Assert.Equal(CardActivityKind.None, adjustment.Kind);
        Assert.Equal(0m, pending.SpendingAmount + adjustment.SpendingAmount);
    }

    [Fact]
    public void Classify_LeavesASavingsTransferOutOfSpendingCashAndDebt()
    {
        var effect = CardActivityRules.Classify(Input(
            AccountTypes.Depository,
            100m,
            SystemCategoryKeys.Transfers,
            SystemGroupKeys.Transfers,
            "Online transfer to savings"));

        Assert.Equal(CardActivityKind.None, effect.Kind);
        Assert.Equal(0m, effect.SpendingAmount);
        Assert.Equal(0m, effect.CashChange);
        Assert.Equal(0m, effect.DebtChange);
    }

    private static CardActivityInput Input(
        string accountType,
        decimal amount,
        string categoryKey,
        string groupKey,
        string name,
        bool pending = false,
        FinancialRecordProvenance provenance = FinancialRecordProvenance.ManualEntry)
    {
        return new CardActivityInput(
            accountType,
            amount,
            pending,
            provenance,
            categoryKey,
            groupKey,
            MerchantName: null,
            name);
    }
}
