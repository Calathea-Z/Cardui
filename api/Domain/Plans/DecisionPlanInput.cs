namespace Cardui.Api.Domain.Plans;

/// <summary>
/// The recovery result a decision plan is written from.
/// Debts are in payoff order. Steps are modeled payoffs only. Missing names are debts left out because the balance is unknown.
/// </summary>
public sealed record DecisionPlanInput(
    DateOnly AsOf,
    string PlanningCurrency,
    Recovery.PayoffRolloverKind Kind,
    DecisionPlanFacts Facts,
    DecisionPlanScenario Scenario,
    IReadOnlyList<DecisionPlanDebt> Debts,
    IReadOnlyList<DecisionPlanStep> Steps,
    IReadOnlyList<string> MissingBalanceNames,
    IReadOnlyList<string> ExcludedCurrencies,
    IReadOnlyList<DecisionPlanUncertainty> Uncertainties);
