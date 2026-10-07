namespace Cardui.Api.Domain.Recovery;

/// <summary>
/// A 30-day cash view and the 6, 12, and 18 month forecasts from one set of inputs.
/// Days are the first 30 days, including days with no events. Horizons are in that month order.
/// Assumptions state the interest and payment rules this result used.
/// ExcludedCurrencies are codes left out of the totals. An empty list means nothing was left out.
/// </summary>
public sealed record CashForecastReport(
    string PlanningCurrency,
    ForecastIncomeBasis IncomeBasis,
    DateOnly AsOf,
    CashWindow DayView,
    IReadOnlyList<CashDay> Days,
    IReadOnlyList<CashHorizon> Horizons,
    IReadOnlyList<ForecastMilestone> Milestones,
    IReadOnlyList<string> ExcludedCurrencies,
    IReadOnlyList<string> Assumptions);
