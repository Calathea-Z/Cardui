using Cardui.Api.Domain.Plans;

namespace Cardui.Api.Dtos.Plan;

/// <summary>
/// One milestone the recovery calculation produced for a decision plan.
/// A null date or amount was not calculated.
/// </summary>
public class DecisionPlanMilestoneDto
{
    public DecisionPlanMilestoneKind Kind { get; set; }

    public string? Name { get; set; }

    public decimal? Amount { get; set; }

    public DateOnly? On { get; set; }
}
