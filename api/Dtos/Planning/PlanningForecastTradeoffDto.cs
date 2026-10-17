using Cardui.Api.Domain.Planning;
using Cardui.Api.Domain.Recovery;

namespace Cardui.Api.Dtos.Planning;

/// <summary>
/// One comparison supplied with the forecast.
/// Path is set for a shortfall or an extra payment. Priority is set for a payoff order.
/// Basis is LowPay when that shortfall used low pay. The explanation does not rank a lower payment as the better result.
/// </summary>
public sealed class PlanningForecastTradeoffDto
{
    public PlanningForecastTradeoffKind Kind { get; init; }

    public PayoffRolloverKind? Path { get; init; }

    public PayoffPriorityChoice? Priority { get; init; }

    public PlanningForecastBasis? Basis { get; init; }

    public required string Explanation { get; init; }
}
