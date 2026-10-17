namespace Cardui.Api.Domain.Planning;

/// <summary>
/// One rule the plan calculation used.
/// Term is the short name already used on Plan. Detail is that rule, including a scenario amount when the calculation applied one.
/// </summary>
public sealed record PlanningForecastAssumption(string Term, string Detail);
