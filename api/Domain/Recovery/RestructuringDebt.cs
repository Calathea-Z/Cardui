using Cardui.Api.Models;

namespace Cardui.Api.Domain.Recovery;

/// <summary>
/// One household debt a comparison may replace in full.
/// Balance is the amount in use. A blank balance, rate, minimum, or due date stays unknown.
/// PromotionalApr applies on the current path when it is in effect. It is not a term of the offer.
/// </summary>
public sealed record RestructuringDebt(
    Guid DebtId,
    string Name,
    string Currency,
    DebtKind Kind,
    decimal? Balance,
    decimal? Apr,
    decimal? PromotionalApr,
    DateOnly? PromotionalEndsOn,
    decimal? MinimumPayment,
    int? RemainingTermMonths,
    DateOnly? NextDueDate);
