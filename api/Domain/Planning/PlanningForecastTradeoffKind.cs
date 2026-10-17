namespace Cardui.Api.Domain.Planning;

/// <summary>
/// Which comparison a forecast explains.
/// Freed cash, payoff priority, a cash shortfall, and an extra payment are the comparisons the plan calculation already makes.
/// The member name is the text a later reader would store. This slice does not store it.
/// </summary>
public enum PlanningForecastTradeoffKind
{
    FreedCash,
    PayoffPriority,
    Shortfall,
    ExtraPayment
}
