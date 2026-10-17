using Cardui.Api.Domain.Planning;
using Cardui.Api.Domain.Plans;
using Cardui.Api.Domain.Recovery;

namespace Cardui.Api.Dtos.Planning;

/// <summary>
/// One difference between the accepted plan and a proposal.
/// A null before-value means there is no accepted plan, or that side has no action.
/// A null after-value means the proposal drops that action. Amounts are the calculator's figures.
/// CalculatedPlan has no amounts: the documents differ and no listed field does.
/// Kind, priority, and constraint fields are set only for that field. Nothing here is saved.
/// </summary>
public sealed class PlanRevisionChangeDto
{
    public PlanRevisionField Field { get; init; }

    public int? Order { get; init; }

    public DecisionPlanActionKind? BeforeAction { get; init; }

    public DecisionPlanActionKind? AfterAction { get; init; }

    public PayoffRolloverKind? BeforeKind { get; init; }

    public PayoffRolloverKind? AfterKind { get; init; }

    public PayoffPriorityChoice? BeforePriority { get; init; }

    public PayoffPriorityChoice? AfterPriority { get; init; }

    public PayoffConstraintKind? BeforeConstraint { get; init; }

    public PayoffConstraintKind? AfterConstraint { get; init; }

    public Guid? BeforeDebtId { get; init; }

    public Guid? AfterDebtId { get; init; }

    public decimal? BeforeAmount { get; init; }

    public decimal? AfterAmount { get; init; }

    public decimal? BeforeExtra { get; init; }

    public decimal? AfterExtra { get; init; }

    public int? BeforeCount { get; init; }

    public int? AfterCount { get; init; }

    public DateOnly? BeforeOn { get; init; }

    public DateOnly? AfterOn { get; init; }

    public string? BeforeName { get; init; }

    public string? AfterName { get; init; }

    public string? BeforeNextName { get; init; }

    public string? AfterNextName { get; init; }

    public bool? BeforeAlreadyShort { get; init; }

    public bool? AfterAlreadyShort { get; init; }
}
