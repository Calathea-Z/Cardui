using Cardui.Api.Domain.Recovery;

namespace Cardui.Api.Domain.Plans;

/// <summary>
/// One saved or previewed decision plan.
/// CalculationVersion identifies the rule that ranked the actions. The same inputs and version produce the same plan.
/// Debts stay in the payoff order captured here. Later household edits do not change a stored copy of this value.
/// </summary>
public sealed record DecisionPlan(
    string CalculationVersion,
    DateOnly AsOf,
    string PlanningCurrency,
    PayoffRolloverKind Kind,
    DecisionPlanFacts Facts,
    DecisionPlanScenario Scenario,
    IReadOnlyList<DecisionPlanDebt> Debts,
    IReadOnlyList<DecisionPlanStep> Steps,
    IReadOnlyList<string> MissingBalanceNames,
    IReadOnlyList<string> ExcludedCurrencies,
    IReadOnlyList<DecisionPlanAction> Actions,
    IReadOnlyList<DecisionPlanMilestone> Milestones,
    IReadOnlyList<DecisionPlanDetail> Assumptions,
    IReadOnlyList<DecisionPlanDetail> Risks,
    IReadOnlyList<DecisionPlanDetail> Missing,
    IReadOnlyList<DecisionPlanDetail> ReviewConditions);
