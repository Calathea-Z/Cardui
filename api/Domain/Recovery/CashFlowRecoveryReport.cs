namespace Cardui.Api.Domain.Recovery;

/// <summary>
/// The cash-flow recovery for rollover, a partial reclaim, and reclaiming all freed cash.
/// Each path shows when a payoff removes a minimum and how much recurring breathing room that leaves.
/// Rollover keeps freed cash committed while a later debt can take it. Reclaim keeps the requested amount
/// back each month, and ReclaimAll keeps every freed dollar. After the last debt stops, freed cash that
/// had been rolling becomes breathing room. ExcludedCurrencies are codes left out.
/// Assumptions state the obligation and breathing-room rules. Nothing is saved.
/// </summary>
public sealed record CashFlowRecoveryReport(
    string PlanningCurrency,
    decimal MonthlyExtra,
    decimal ReclaimAmount,
    CashFlowRecoveryPath Rollover,
    CashFlowRecoveryPath Reclaim,
    CashFlowRecoveryPath ReclaimAll,
    IReadOnlyList<string> ExcludedCurrencies,
    IReadOnlyList<string> Assumptions);
