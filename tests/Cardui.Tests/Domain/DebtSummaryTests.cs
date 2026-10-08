using Cardui.Api.Domain.Debts;
using Cardui.Api.Models;
using Xunit;

namespace Cardui.Tests.Domain;

public class DebtSummaryTests
{
    private static readonly DateOnly Today = new(2026, 10, 5);

    [Fact]
    public void Calculate_EstimatesOneMonthOfInterestAndLeavesUnknownRatesOut()
    {
        var known = Card(balance: 842.50m, apr: 19.99m);
        var zero = Card(nameId: 2, balance: 0m, apr: 24m);
        var missingRate = Card(nameId: 3, balance: 100m, apr: null);
        var missingBalance = Card(nameId: 4, balance: null, apr: 10m, balanceAsOf: null);

        var report = DebtSummary.Calculate([known, zero, missingRate, missingBalance], Today);

        var item = Assert.Single(report.Debts, debt => debt.DebtId == known.DebtId);
        Assert.Equal(14.03m, item.MonthlyInterest);
        Assert.Equal(19.99m, item.RateInEffect);
        Assert.False(item.RateIsPromotional);
        Assert.False(item.AprReachesNotice);
        Assert.DoesNotContain(DebtSummaryGap.Apr, item.Gaps);

        Assert.Equal(0m, report.Debts.Single(debt => debt.DebtId == zero.DebtId).MonthlyInterest);
        Assert.False(report.Debts.Single(debt => debt.DebtId == zero.DebtId).AprReachesNotice);
        Assert.Null(report.Debts.Single(debt => debt.DebtId == missingRate.DebtId).MonthlyInterest);
        Assert.Contains(
            DebtSummaryGap.Apr,
            report.Debts.Single(debt => debt.DebtId == missingRate.DebtId).Gaps);
        Assert.Null(report.Debts.Single(debt => debt.DebtId == missingBalance.DebtId).MonthlyInterest);

        var totals = Assert.Single(report.Currencies);
        Assert.Equal(942.50m, totals.RecordedBalance);
        Assert.Equal(1, totals.UnknownBalanceCount);
        Assert.Equal(14.03m, totals.MonthlyInterest);
        Assert.Equal(1, totals.UnknownInterestCount);
        Assert.Equal(20m, report.AprNoticePercent);
        Assert.Equal(0.30m, report.UtilizationNotice);
        Assert.Equal(0.90m, report.UtilizationLimitNotice);
        Assert.Equal(60, report.PromotionalNoticeDays);
    }

    [Fact]
    public void Calculate_UsesThePromotionalRateThroughItsEndDate()
    {
        var current = Card(
            promotionalApr: 0m,
            promotionalEndsOn: Today,
            apr: 24m);
        var ended = Card(
            nameId: 2,
            promotionalApr: 0m,
            promotionalEndsOn: Today.AddDays(-1),
            apr: 24m);
        var openEnded = Card(
            nameId: 3,
            promotionalApr: 0m,
            promotionalEndsOn: null,
            apr: null);
        var endingSoon = Card(
            nameId: 4,
            promotionalApr: 1m,
            promotionalEndsOn: Today.AddDays(60),
            apr: 22m);
        var endingLater = Card(
            nameId: 5,
            promotionalApr: 1m,
            promotionalEndsOn: Today.AddDays(61),
            apr: 22m);

        var report = DebtSummary.Calculate(
            [current, ended, openEnded, endingSoon, endingLater],
            Today);

        var currentItem = Item(report, current.DebtId);
        Assert.Equal(0m, currentItem.RateInEffect);
        Assert.True(currentItem.RateIsPromotional);
        Assert.False(currentItem.PromotionEnded);
        Assert.True(currentItem.PromoEndsWithinNotice);
        Assert.DoesNotContain(DebtSummaryGap.RateAfterPromotion, currentItem.Gaps);

        var endedItem = Item(report, ended.DebtId);
        Assert.Equal(24m, endedItem.RateInEffect);
        Assert.False(endedItem.RateIsPromotional);
        Assert.True(endedItem.PromotionEnded);
        Assert.False(endedItem.PromoEndsWithinNotice);
        Assert.True(endedItem.AprReachesNotice);

        var openItem = Item(report, openEnded.DebtId);
        Assert.Equal(0m, openItem.MonthlyInterest);
        Assert.True(openItem.RateIsPromotional);
        Assert.Contains(DebtSummaryGap.PromotionalEnd, openItem.Gaps);
        Assert.Contains(DebtSummaryGap.RateAfterPromotion, openItem.Gaps);

        Assert.True(Item(report, endingSoon.DebtId).PromoEndsWithinNotice);
        Assert.False(Item(report, endingLater.DebtId).PromoEndsWithinNotice);

        var totals = Assert.Single(report.Currencies);
        Assert.Equal(2, totals.PromoEndingCount);
        Assert.Equal(1, totals.PromotionEndedCount);
        Assert.Equal(1, totals.AprNoticeCount);
        Assert.Equal(1, totals.MissingPromotionalEndCount);
        Assert.Equal(1, totals.MissingRateAfterPromotionCount);
    }

    [Fact]
    public void Calculate_CallsOutAPassedDueDateAndLeavesTodayUnpaidStatusUnknown()
    {
        var passed = Card(nextDueDate: Today.AddDays(-1));
        var dueToday = Card(nameId: 2, nextDueDate: Today);
        var missing = Card(nameId: 3, nextDueDate: null);

        var report = DebtSummary.Calculate([passed, dueToday, missing], Today);

        Assert.True(Item(report, passed.DebtId).DueDatePassed);
        Assert.False(Item(report, dueToday.DebtId).DueDatePassed);
        Assert.DoesNotContain(DebtSummaryGap.DueDate, Item(report, dueToday.DebtId).Gaps);
        Assert.False(Item(report, missing.DebtId).DueDatePassed);
        Assert.Contains(DebtSummaryGap.DueDate, Item(report, missing.DebtId).Gaps);

        var totals = Assert.Single(report.Currencies);
        Assert.Equal(1, totals.DueDatePassedCount);
        Assert.Equal(1, totals.MissingDueDateCount);
    }

    [Fact]
    public void Calculate_NamesUtilizationThresholdsAndSkipsIncompleteRevolvingDebts()
    {
        var notice = Card(balance: 300m, creditLimit: 1000m);
        var nearLimit = Card(nameId: 2, balance: 900m, creditLimit: 1000m);
        var under = Card(nameId: 3, balance: 299m, creditLimit: 1000m);
        var incomplete = Card(nameId: 4, balance: 50m, creditLimit: null);
        var loan = Card(
            nameId: 5,
            kind: DebtKind.Installment,
            balance: 500m,
            creditLimit: null,
            remainingTermMonths: null);

        var report = DebtSummary.Calculate([notice, nearLimit, under, incomplete, loan], Today);

        Assert.True(Item(report, notice.DebtId).UtilizationReachesNotice);
        Assert.False(Item(report, notice.DebtId).UtilizationReachesLimitNotice);
        Assert.True(Item(report, nearLimit.DebtId).UtilizationReachesLimitNotice);
        Assert.False(Item(report, nearLimit.DebtId).UtilizationReachesNotice);
        Assert.False(Item(report, under.DebtId).UtilizationReachesNotice);
        Assert.Contains(DebtSummaryGap.CreditLimit, Item(report, incomplete.DebtId).Gaps);
        Assert.Contains(DebtSummaryGap.RemainingTerm, Item(report, loan.DebtId).Gaps);
        Assert.DoesNotContain(DebtSummaryGap.CreditLimit, Item(report, loan.DebtId).Gaps);

        var totals = Assert.Single(report.Currencies);
        Assert.Equal(0.4997m, totals.Utilization);
        Assert.Equal(3, totals.UtilizationDebtCount);
        Assert.Equal(1, totals.UnknownUtilizationCount);
        Assert.Equal(1, totals.UtilizationNoticeCount);
        Assert.Equal(1, totals.UtilizationLimitNoticeCount);
    }

    [Fact]
    public void Calculate_KeepsKnownZerosAndDoesNotAddOtherCurrencies()
    {
        var usd = Card(balance: 0m, minimumPayment: 0m, apr: 0m);
        var unknownMinimum = Card(nameId: 2, balance: 40m, minimumPayment: null, apr: null);
        var cad = Card(nameId: 3, currency: "CAD", balance: 25m, minimumPayment: 5m, apr: 10m);

        var report = DebtSummary.Calculate([usd, unknownMinimum, cad], Today);

        var dollars = report.Currencies.Single(group => group.Currency == "USD");
        Assert.Equal(40m, dollars.RecordedBalance);
        Assert.Equal(0, dollars.UnknownBalanceCount);
        Assert.Null(dollars.MinimumPayments);
        Assert.Equal(1, dollars.UnknownMinimumCount);
        Assert.Null(dollars.MonthlyInterest);

        var canada = report.Currencies.Single(group => group.Currency == "CAD");
        Assert.Equal(25m, canada.RecordedBalance);
        Assert.Equal(5m, canada.MinimumPayments);
        Assert.Equal(2, report.Currencies.Count);
    }

    [Fact]
    public void Calculate_KeepsZeroBalanceTermsForReviewAndExcludesThemFromActiveTotals()
    {
        var active = Card(balance: 500m, minimumPayment: 50m, apr: 18m, creditLimit: 1000m);
        var paid = Card(
            nameId: 2,
            balance: 0m,
            minimumPayment: 125m,
            apr: 29m,
            creditLimit: 1000m);

        var report = DebtSummary.Calculate([active, paid], Today);
        var totals = Assert.Single(report.Currencies);
        var paidItem = Item(report, paid.DebtId);

        Assert.Equal(1, totals.DebtCount);
        Assert.Equal(50m, totals.MinimumPayments);
        Assert.Equal(0, totals.AprNoticeCount);
        Assert.Equal(1, totals.UtilizationDebtCount);
        Assert.Equal(1, totals.ZeroBalancePaymentReviewCount);
        Assert.True(paidItem.NeedsPaymentReview);
        Assert.Empty(paidItem.Gaps);
    }

    [Fact]
    public void Calculate_ShowsADifferentLiabilityBalanceAndKeepsTheRecordedOneForInterest()
    {
        var linked = new DebtLinkedBalance(900m, new DateOnly(2026, 10, 4), "USD", true);
        var debt = Card(balance: 842.50m, apr: 19.99m, linked: linked);
        var same = Card(nameId: 2, balance: 900m, linked: linked);
        var unknown = Card(nameId: 3, balance: null, balanceAsOf: null, linked: linked);
        var cash = Card(
            nameId: 4,
            balance: 50m,
            linked: new DebtLinkedBalance(100m, Today, "USD", false));

        var report = DebtSummary.Calculate([debt, same, unknown, cash], Today);

        var comparison = Item(report, debt.DebtId).BalanceComparison;
        Assert.NotNull(comparison);
        Assert.Equal(900m, comparison.AccountBalance);
        Assert.Equal(new DateOnly(2026, 10, 4), comparison.AccountBalanceAsOf);
        Assert.Equal("USD", comparison.AccountCurrency);
        Assert.True(comparison.CanUseAccountBalance);
        Assert.Equal(DebtAccountBalanceBlock.None, comparison.Block);
        Assert.Equal(14.03m, Item(report, debt.DebtId).MonthlyInterest);

        Assert.Null(Item(report, same.DebtId).BalanceComparison);
        var missing = Item(report, unknown.DebtId);
        Assert.NotNull(missing.BalanceComparison);
        Assert.True(missing.BalanceComparison.CanUseAccountBalance);
        Assert.Null(missing.MonthlyInterest);
        Assert.Null(Item(report, cash.DebtId).BalanceComparison);

        Assert.Equal(2, Assert.Single(report.Currencies).BalanceDifferenceCount);
    }

    [Fact]
    public void Calculate_RefusesAnAccountBalanceThatCannotBeStored()
    {
        var undated = Card(linked: new DebtLinkedBalance(900m, null, "USD", true));
        var negative = Card(
            nameId: 2,
            linked: new DebtLinkedBalance(-5m, Today, "USD", true));
        var otherCurrency = Card(
            nameId: 3,
            linked: new DebtLinkedBalance(900m, Today, "CAD", true));
        var blankCurrency = Card(
            nameId: 4,
            linked: new DebtLinkedBalance(900m, Today, "  ", true));

        var report = DebtSummary.Calculate([undated, negative, otherCurrency, blankCurrency], Today);

        Assert.Equal(DebtAccountBalanceBlock.DateUnknown, Comparison(report, undated).Block);
        Assert.False(Comparison(report, undated).CanUseAccountBalance);
        Assert.Equal(DebtAccountBalanceBlock.NegativeBalance, Comparison(report, negative).Block);
        Assert.Equal(DebtAccountBalanceBlock.CurrencyDiffers, Comparison(report, otherCurrency).Block);
        Assert.Equal(DebtAccountBalanceBlock.CurrencyDiffers, Comparison(report, blankCurrency).Block);

        Assert.False(DebtSummary.TryReadChosenBalance(
            "USD",
            undated.LinkedBalance,
            out _,
            out _,
            out var undatedError));
        Assert.Equal("That account balance has no date, so it is not copied.", undatedError);

        Assert.False(DebtSummary.TryReadChosenBalance(
            "USD",
            new DebtLinkedBalance(10m, Today, "USD", false),
            out _,
            out _,
            out var cashError));
        Assert.Equal("That account balance is not an amount owed, so it is not copied.", cashError);

        Assert.True(DebtSummary.TryReadChosenBalance(
            "usd",
            new DebtLinkedBalance(900.004m, Today, "USD", true),
            out var chosen,
            out var asOf,
            out var error));
        Assert.Equal("", error);
        Assert.Equal(900.00m, chosen);
        Assert.Equal(Today, asOf);
    }

    [Fact]
    public void Calculate_ReturnsAnEmptyInventoryWhenThereAreNoDebts()
    {
        var report = DebtSummary.Calculate([], Today);

        Assert.Empty(report.Debts);
        Assert.Empty(report.Currencies);
    }

    private static DebtSummaryItem Item(DebtSummaryReport report, Guid debtId)
    {
        return Assert.Single(report.Debts, debt => debt.DebtId == debtId);
    }

    private static DebtBalanceComparison Comparison(DebtSummaryReport report, DebtSummaryInput debt)
    {
        var comparison = Item(report, debt.DebtId).BalanceComparison;
        Assert.NotNull(comparison);
        return comparison;
    }

    private static DebtSummaryInput Card(
        int nameId = 1,
        string currency = "USD",
        DebtKind kind = DebtKind.Revolving,
        decimal? balance = 100m,
        DateOnly? balanceAsOf = null,
        decimal? apr = null,
        decimal? minimumPayment = null,
        DateOnly? nextDueDate = null,
        decimal? creditLimit = null,
        int? remainingTermMonths = null,
        decimal? promotionalApr = null,
        DateOnly? promotionalEndsOn = null,
        DebtLinkedBalance? linked = null)
    {
        return new DebtSummaryInput(
            GuidFrom(nameId),
            currency,
            kind,
            balance,
            balance is null ? null : balanceAsOf ?? new DateOnly(2026, 10, 1),
            apr,
            minimumPayment,
            nextDueDate,
            creditLimit,
            remainingTermMonths,
            promotionalApr,
            promotionalEndsOn,
            linked);
    }

    private static Guid GuidFrom(int value)
    {
        return Guid.Parse($"00000000-0000-0000-0000-{value:000000000000}");
    }
}
