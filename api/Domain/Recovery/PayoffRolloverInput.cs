namespace Cardui.Api.Domain.Recovery;

/// <summary>
/// The debts, the payoff order, and how much freed cash to keep.
/// MonthlyExtra is shared extra directed at the first debt that can take it. It is not part of a freed payment.
/// Order is the caller's ranking. An empty list uses avalanche. Debts left off a partial list follow avalanche.
/// ReclaimAmount is how much of the freed cash to keep each month for savings or spending.
/// Zero keeps nothing, so that path matches rollover. An amount above the freed cash that month keeps all of it.
/// AsOf is the first date a payment can fall on. A stored due date before it is not replayed: the first payment is the
/// first monthly date on or after AsOf, still stepped from the stored date, and the balance stays as given until then.
/// </summary>
public sealed record PayoffRolloverInput(
    string PlanningCurrency,
    decimal MonthlyExtra,
    IReadOnlyList<PayoffDebt> Debts,
    IReadOnlyList<Guid> Order,
    decimal ReclaimAmount,
    DateOnly AsOf);
