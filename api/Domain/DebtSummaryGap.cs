namespace Cardui.Api.Domain;

/// <summary>
/// A fact the summary view needs and does not have.
/// A blank term stays a gap. A known zero is not a gap.
/// </summary>
public enum DebtSummaryGap
{
    Balance,
    Apr,
    MinimumPayment,
    DueDate,
    CreditLimit,
    RemainingTerm,
    PromotionalEnd,
    PromotionalRate,
    RateAfterPromotion
}
