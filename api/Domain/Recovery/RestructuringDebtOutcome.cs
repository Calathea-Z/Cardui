using Cardui.Api.Models;

namespace Cardui.Api.Domain.Recovery;

/// <summary>
/// How one replaced debt projects on its current minimum.
/// Apr is the stored rate. A promotional rate is separate and is not folded into Apr.
/// PaidOffOn is null when Stop is not PaidOff.
/// </summary>
public sealed record RestructuringDebtOutcome(
    Guid DebtId,
    string Name,
    DebtKind Kind,
    decimal Balance,
    decimal? Apr,
    decimal? PromotionalApr,
    DateOnly? PromotionalEndsOn,
    DebtScheduleStop Stop,
    DateOnly? PaidOffOn);
