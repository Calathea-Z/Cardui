using Cardui.Api.Domain.Debts;

namespace Cardui.Api.Dtos.Debts;

public class DebtSummaryItemDto
{
    public Guid DebtId { get; set; }

    /// <summary>
    /// One month of interest on the recorded balance. Null when the balance or the rate is unknown.
    /// This is an estimate, not the total interest left to pay.
    /// </summary>
    public decimal? MonthlyInterest { get; set; }

    /// <summary>
    /// The rate used for this month's interest, as a percent. Null when that rate is unknown.
    /// </summary>
    public decimal? RateInEffect { get; set; }

    /// <summary>
    /// True when the interest estimate uses the promotional APR.
    /// </summary>
    public bool RateIsPromotional { get; set; }

    public bool PromotionEnded { get; set; }

    public DateOnly? PromotionalEndsOn { get; set; }

    /// <summary>
    /// True when the promotion ends today or within the notice window, and has not ended yet.
    /// </summary>
    public bool PromoEndsWithinNotice { get; set; }

    /// <summary>
    /// True when the recorded due date is before today.
    /// This does not say whether the payment was made.
    /// </summary>
    public bool DueDatePassed { get; set; }

    /// <summary>
    /// True when the rate in effect is at or above the named APR notice.
    /// </summary>
    public bool AprReachesNotice { get; set; }

    /// <summary>
    /// True when utilization is at the notice and below the limit notice.
    /// </summary>
    public bool UtilizationReachesNotice { get; set; }

    /// <summary>
    /// True when utilization is at or above the limit notice.
    /// </summary>
    public bool UtilizationReachesLimitNotice { get; set; }

    public IReadOnlyList<DebtSummaryGap> Gaps { get; set; } = [];

    /// <summary>
    /// Set when a linked card or loan balance differs from the recorded balance.
    /// Null when they match, or when the account is not an amount owed.
    /// </summary>
    public DebtBalanceComparisonDto? BalanceComparison { get; set; }
}
