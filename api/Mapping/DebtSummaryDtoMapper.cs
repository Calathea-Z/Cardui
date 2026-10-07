using Cardui.Api.Domain.Debts;
using Cardui.Api.Dtos.Debts;

namespace Cardui.Api.Mapping;

public static class DebtSummaryDtoMapper
{
    /// <summary>
    /// Copies a summary report into the API shape.
    /// The notice numbers travel with the report so the screen names the same thresholds.
    /// </summary>
    public static DebtSummaryReportDto Map(DebtSummaryReport report)
    {
        return new DebtSummaryReportDto
        {
            AprNoticePercent = report.AprNoticePercent,
            UtilizationNotice = report.UtilizationNotice,
            UtilizationLimitNotice = report.UtilizationLimitNotice,
            PromotionalNoticeDays = report.PromotionalNoticeDays,
            Currencies = report.Currencies.Select(MapCurrency).ToList(),
            Debts = report.Debts.Select(MapItem).ToList()
        };
    }

    #region Private Methods

    /// <summary>
    /// Copies one currency's totals.
    /// </summary>
    private static DebtCurrencySummaryDto MapCurrency(DebtCurrencySummary currency)
    {
        return new DebtCurrencySummaryDto
        {
            Currency = currency.Currency,
            DebtCount = currency.DebtCount,
            RecordedBalance = currency.RecordedBalance,
            UnknownBalanceCount = currency.UnknownBalanceCount,
            MonthlyInterest = currency.MonthlyInterest,
            UnknownInterestCount = currency.UnknownInterestCount,
            MinimumPayments = currency.MinimumPayments,
            UnknownMinimumCount = currency.UnknownMinimumCount,
            Utilization = currency.Utilization,
            UtilizationDebtCount = currency.UtilizationDebtCount,
            UnknownUtilizationCount = currency.UnknownUtilizationCount,
            DueDatePassedCount = currency.DueDatePassedCount,
            PromoEndingCount = currency.PromoEndingCount,
            PromotionEndedCount = currency.PromotionEndedCount,
            AprNoticeCount = currency.AprNoticeCount,
            UtilizationNoticeCount = currency.UtilizationNoticeCount,
            UtilizationLimitNoticeCount = currency.UtilizationLimitNoticeCount,
            BalanceDifferenceCount = currency.BalanceDifferenceCount,
            MissingDueDateCount = currency.MissingDueDateCount,
            MissingRemainingTermCount = currency.MissingRemainingTermCount,
            MissingPromotionalEndCount = currency.MissingPromotionalEndCount,
            MissingPromotionalRateCount = currency.MissingPromotionalRateCount,
            MissingRateAfterPromotionCount = currency.MissingRateAfterPromotionCount
        };
    }

    /// <summary>
    /// Copies one debt's summary. A missing comparison stays null.
    /// </summary>
    private static DebtSummaryItemDto MapItem(DebtSummaryItem item)
    {
        return new DebtSummaryItemDto
        {
            DebtId = item.DebtId,
            MonthlyInterest = item.MonthlyInterest,
            RateInEffect = item.RateInEffect,
            RateIsPromotional = item.RateIsPromotional,
            PromotionEnded = item.PromotionEnded,
            PromotionalEndsOn = item.PromotionalEndsOn,
            PromoEndsWithinNotice = item.PromoEndsWithinNotice,
            DueDatePassed = item.DueDatePassed,
            AprReachesNotice = item.AprReachesNotice,
            UtilizationReachesNotice = item.UtilizationReachesNotice,
            UtilizationReachesLimitNotice = item.UtilizationReachesLimitNotice,
            Gaps = item.Gaps,
            BalanceComparison = item.BalanceComparison is null
                ? null
                : new DebtBalanceComparisonDto
                {
                    AccountBalance = item.BalanceComparison.AccountBalance,
                    AccountBalanceAsOf = item.BalanceComparison.AccountBalanceAsOf,
                    AccountCurrency = item.BalanceComparison.AccountCurrency,
                    CanUseAccountBalance = item.BalanceComparison.CanUseAccountBalance,
                    Block = item.BalanceComparison.Block
                }
        };
    }

    #endregion
}
