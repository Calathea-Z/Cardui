namespace Cardui.Api.Domain.Planning;

/// <summary>
/// A forecast whose amounts and dates were copied from the plan calculation.
/// CalculationVersion names this wording. Figures are the copied numbers. Assumptions are the rules those numbers used.
/// Tradeoffs name both sides. Names are the debt and income names copied with the calculation, so a digit in a name is not a new amount.
/// Statement is the wording that may be said. It is an estimate, not a promise.
/// </summary>
public sealed record PlanningForecast(
    string CalculationVersion,
    string PlanningCurrency,
    IReadOnlyList<PlanningForecastFigure> Figures,
    IReadOnlyList<PlanningForecastAssumption> Assumptions,
    IReadOnlyList<PlanningForecastTradeoff> Tradeoffs,
    IReadOnlyList<string> Names,
    string Statement);
