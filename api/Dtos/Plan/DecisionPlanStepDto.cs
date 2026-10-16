namespace Cardui.Api.Dtos.Plan;

/// <summary>
/// One modeled payoff captured for a decision plan.
/// StartsOn is null when the freed-payment date falls outside the projection.
/// </summary>
public class DecisionPlanStepDto
{
    public Guid DebtId { get; set; }

    public required string Name { get; set; }

    public DateOnly EndedOn { get; set; }

    public DateOnly? StartsOn { get; set; }

    public decimal Minimum { get; set; }

    public decimal BreathingRoom { get; set; }
}
