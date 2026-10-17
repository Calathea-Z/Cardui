using Cardui.Api.Domain.Recovery;

namespace Cardui.Api.Domain.Planning;

/// <summary>
/// One payoff order the calculation compared.
/// Applied means this estimate follows that order. ChangesOrder means it is not the highest-rate order.
/// </summary>
public sealed record PlanningForecastPriority(
    PayoffPriorityChoice Choice,
    bool Applied,
    bool ChangesOrder);
