namespace Cardui.Api.Domain.Recovery;

/// <summary>
/// The debts and the extra payment a payoff comparison ranks.
/// MonthlyExtra is shared extra directed by the order under comparison. It is not carried to the next month when a month does not use it.
/// UserOrder is the caller's ranking. An empty list means no selection, and that alternative then matches avalanche.
/// Debts left off UserOrder follow avalanche after the ones that were listed.
/// Constraints change the recommended order. They do not replace the user-selected alternative.
/// </summary>
public sealed record PayoffPriorityInput(
    string PlanningCurrency,
    decimal MonthlyExtra,
    IReadOnlyList<PayoffDebt> Debts,
    IReadOnlyList<Guid> UserOrder,
    IReadOnlyList<PayoffConstraint> Constraints);
