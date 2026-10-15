using Cardui.Api.Domain.Recovery;
using Cardui.Api.Models;

namespace Cardui.Api.Dtos.Plan;

/// <summary>
/// One replaced debt on its current minimum.
/// Apr is the stored rate. PromotionalApr is separate. PaidOffOn is null when Stop is not PaidOff.
/// </summary>
public sealed class RestructuringDebtOutcomeDto
{
    public Guid DebtId { get; set; }

    public required string Name { get; set; }

    public DebtKind Kind { get; set; }

    public decimal Balance { get; set; }

    public decimal? Apr { get; set; }

    public decimal? PromotionalApr { get; set; }

    public DateOnly? PromotionalEndsOn { get; set; }

    public DebtScheduleStop Stop { get; set; }

    public DateOnly? PaidOffOn { get; set; }
}
