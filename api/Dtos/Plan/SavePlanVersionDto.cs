using Cardui.Api.Domain.Recovery;

namespace Cardui.Api.Dtos.Plan;

/// <summary>
/// The scenario to store as a draft, and the draft this request expects to replace.
/// ReplacesDraftId is null only for the first draft. Kind is roll payments forward or free up cash.
/// </summary>
public class SavePlanVersionDto
{
    public PayoffRolloverKind Kind { get; set; }

    public decimal MonthlyExtra { get; set; }

    public int IncomeLossMonths { get; set; }

    public decimal WindfallAmount { get; set; }

    public DateOnly? WindfallOn { get; set; }

    public decimal? LivingSpendingMonthly { get; set; }

    public decimal? CashFloor { get; set; }

    public Guid? ReplacesDraftId { get; set; }
}
