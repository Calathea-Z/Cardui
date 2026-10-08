namespace Cardui.Api.Domain.Debts;

/// <summary>
/// Summary of one debt. There is no score.
/// MonthlyInterest is null when the balance or the rate in effect is unknown. Zero is a known zero.
/// RateIsPromotional means the interest estimate uses the promotional APR.
/// UtilizationReachesNotice is 30 percent or more and under 90. UtilizationReachesLimitNotice is 90 percent or more.
/// BalanceComparison is null when there is nothing to set beside the recorded balance.
/// NeedsPaymentReview keeps a saved positive minimum visible on a known zero balance without treating it as active.
/// </summary>
public sealed record DebtSummaryItem(
    Guid DebtId,
    string Currency,
    decimal? MonthlyInterest,
    decimal? RateInEffect,
    bool RateIsPromotional,
    bool PromotionEnded,
    DateOnly? PromotionalEndsOn,
    bool PromoEndsWithinNotice,
    bool DueDatePassed,
    bool AprReachesNotice,
    bool UtilizationReachesNotice,
    bool UtilizationReachesLimitNotice,
    bool NeedsPaymentReview,
    IReadOnlyList<DebtSummaryGap> Gaps,
    DebtBalanceComparison? BalanceComparison);
