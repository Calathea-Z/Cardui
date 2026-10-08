namespace Cardui.Api.Domain.Debts;

/// <summary>
/// Totals for debts that share one currency.
/// A null total means every amount in that total is unknown. A known zero stays zero and is not a missing amount.
/// DebtCount, minimums, utilization, and risk counts include only debts with a positive balance.
/// ZeroBalancePaymentReviewCount keeps saved positive minimums visible without treating those debts as active.
/// StaleCount is how many followed debts in this currency are not current. A stale balance is still in the totals.
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
    int MissingRateAfterPromotionCount,
    int StaleCount,
    int ZeroBalancePaymentReviewCount);
