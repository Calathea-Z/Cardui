using Cardui.Api.Dtos.Plan;

namespace Cardui.Api.Dtos.Planning;

/// <summary>
/// A drafted plan change. Applied is false. The draft is not saved and not accepted.
/// </summary>
public sealed class PlanChangeDraftDto
{
    public bool Applied { get; init; }

    public required DecisionPlanDto Plan { get; init; }
}
