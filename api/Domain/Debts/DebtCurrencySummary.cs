namespace Cardui.Api.Domain.Debts;

/// <summary>
/// Totals for debts that share one currency.
/// A null total means every amount in that total is unknown. A known zero stays zero and is not a missing amount.
/// Debts in another currency are not added in.
/// </summary>
public sealed record DebtCurrencySummary(
    string Currency,
    int DebtCount,
    decimal? RecordedBalance,
    int UnknownBalanceCount,
    decimal? MonthlyInterest,
    int UnknownInterestCount,
    decimal? MinimumPayments,
    int UnknownMinimumCount,
    decimal? Utilization,
    int UtilizationDebtCount,
    int UnknownUtilizationCount,
    int DueDatePassedCount,
    int PromoEndingCount,
    int PromotionEndedCount,
    int AprNoticeCount,
    int UtilizationNoticeCount,
    int UtilizationLimitNoticeCount,
    int BalanceDifferenceCount,
    int MissingDueDateCount,
    int MissingRemainingTermCount,
    int MissingPromotionalEndCount,
    int MissingPromotionalRateCount,
    int MissingRateAfterPromotionCount);
