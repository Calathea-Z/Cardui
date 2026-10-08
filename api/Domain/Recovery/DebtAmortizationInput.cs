using Cardui.Api.Models;

namespace Cardui.Api.Domain.Recovery;

/// <summary>
/// The terms used to project one debt.
/// Balance is the amount owed at the start. A blank APR, minimum, or due date stays unknown.
/// ExtraPayment is added to this debt each month until it is paid off. It is not moved to another debt.
/// RemainingTermMonths is used only to calculate a level payment when the minimum is blank.
/// </summary>
public sealed record DebtAmortizationInput(
    Guid DebtId,
    string Name,
    string Currency,
    DebtKind Kind,
    decimal Balance,
    decimal? Apr,
    decimal? PromotionalApr,
    DateOnly? PromotionalEndsOn,
    decimal? MinimumPayment,
    int? RemainingTermMonths,
    DateOnly? NextDueDate,
    decimal ExtraPayment);
