using Cardui.Api.Domain.Recovery;

namespace Cardui.Api.Dtos.Plan;

/// <summary>
/// A decision plan: the ranked actions and the facts they were written from.
/// Amounts are in the planning currency. This document does not change when the household later edits a debt or a bill.
/// </summary>
public class DecisionPlanDto
{
    public required string CalculationVersion { get; set; }

    public DateOnly AsOf { get; set; }

    public required string PlanningCurrency { get; set; }

    public PayoffRolloverKind Kind { get; set; }

    public required DecisionPlanFactsDto Facts { get; set; }

    public required DecisionPlanScenarioDto Scenario { get; set; }

    public IReadOnlyList<DecisionPlanDebtDto> Debts { get; set; } = [];

    public IReadOnlyList<DecisionPlanStepDto> Steps { get; set; } = [];

    public IReadOnlyList<string> MissingBalanceNames { get; set; } = [];

    public IReadOnlyList<string> ExcludedCurrencies { get; set; } = [];

    public IReadOnlyList<DecisionPlanActionDto> Actions { get; set; } = [];

    public IReadOnlyList<DecisionPlanMilestoneDto> Milestones { get; set; } = [];

    public IReadOnlyList<DecisionPlanDetailDto> Assumptions { get; set; } = [];

    public IReadOnlyList<DecisionPlanDetailDto> Risks { get; set; } = [];

    public IReadOnlyList<DecisionPlanDetailDto> Missing { get; set; } = [];

    public IReadOnlyList<DecisionPlanDetailDto> ReviewConditions { get; set; } = [];
}
