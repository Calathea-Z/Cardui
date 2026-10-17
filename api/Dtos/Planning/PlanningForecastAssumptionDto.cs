namespace Cardui.Api.Dtos.Planning;

/// <summary>
/// One rule the plan calculation used.
/// Term is the short name. Detail is the rule, including a scenario amount when the calculation applied one.
/// </summary>
public sealed class PlanningForecastAssumptionDto
{
    public required string Term { get; init; }

    public required string Detail { get; init; }
}
