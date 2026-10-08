namespace Cardui.Api.Domain.Recovery;

/// <summary>
/// Rollover, a partial reclaim, and reclaiming all freed cash for one set of debts.
/// Rollover is the default: a freed minimum and that debt's planned extra go to the next debt after the payment ends.
/// Reclaim keeps ReclaimAmount of that cash each month for savings or spending. ReclaimAll keeps every freed dollar.
/// OrderProvided is false when no order was sent, and the paths then follow avalanche.
/// ExcludedCurrencies are codes left out. Assumptions state the rollover and interest rules.
/// The same inputs produce the same paths and the same cents. Nothing is saved.
/// </summary>
public sealed record PayoffRolloverComparison(
    string PlanningCurrency,
    decimal MonthlyExtra,
    decimal ReclaimAmount,
    bool OrderProvided,
    PayoffRolloverPath Rollover,
    PayoffRolloverPath Reclaim,
    PayoffRolloverPath ReclaimAll,
    IReadOnlyList<string> ExcludedCurrencies,
    IReadOnlyList<string> Assumptions);
