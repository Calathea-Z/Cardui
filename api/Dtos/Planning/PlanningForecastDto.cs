namespace Cardui.Api.Dtos.Planning;

/// <summary>
/// A forecast whose amounts and dates were copied from the plan calculation.
/// CalculationVersion names the wording. Figures are the copied numbers. Assumptions are the rules those numbers used.
/// Nothing in this response was saved. The statement is an estimate, not a promise.
/// </summary>
public sealed class PlanningForecastDto
{
    public required string CalculationVersion { get; init; }

    public required string PlanningCurrency { get; init; }

    public IReadOnlyList<PlanningForecastFigureDto> Figures { get; init; } = [];

    public IReadOnlyList<PlanningForecastAssumptionDto> Assumptions { get; init; } = [];

    public IReadOnlyList<PlanningForecastTradeoffDto> Tradeoffs { get; init; } = [];

    /// <summary>
    /// Debt and income names copied with the calculation.
    /// A digit inside one of these names is not a forecast amount.
    /// </summary>
    public IReadOnlyList<string> Names { get; init; } = [];

    public required string Statement { get; init; }
}
