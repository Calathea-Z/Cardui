using Cardui.Api.Dtos.Plan;

namespace Cardui.Api.Dtos.Planning;

/// <summary>
/// Two calculated scenarios. Applied is false. Neither scenario is saved.
/// Baseline is the first request. Alternative is the second. Both use the same household facts.
/// </summary>
public sealed class PlanningScenarioComparisonDto
{
    public bool Applied { get; init; }

    public required PlanRecoveryDto Baseline { get; init; }

    public required PlanRecoveryDto Alternative { get; init; }
}
