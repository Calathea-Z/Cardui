using Cardui.Api.Domain.Recovery;

namespace Cardui.Api.Domain.Planning;

/// <summary>
/// One comparison, in the words that may be said with the forecast.
/// Path is set for a shortfall or an extra payment on one freed-cash choice. Priority is set when the comparison is a payoff order.
/// Basis is LowPay when the shortfall used low pay. The explanation uses amounts copied from the calculation.
/// It does not rank a lower payment as the better result.
/// </summary>
public sealed record PlanningForecastTradeoff(
    PlanningForecastTradeoffKind Kind,
    PayoffRolloverKind? Path,
    PayoffPriorityChoice? Priority,
    PlanningForecastBasis? Basis,
    string Explanation);
