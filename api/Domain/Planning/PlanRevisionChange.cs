using Cardui.Api.Domain.Plans;
using Cardui.Api.Domain.Recovery;

namespace Cardui.Api.Domain.Planning;

/// <summary>
/// One difference between the accepted plan and a proposal.
/// A null before-value means the household has no accepted plan, or that side has no action.
/// A null after-value means the proposal drops that action. Amounts are the calculator's figures.
/// CalculatedPlan has no amounts: the documents differ and no listed field does.
/// Kind, priority, and constraint fields are set only for that <see cref="PlanRevisionField"/>.
/// Already-short flags are set only for an action that exists on that side.
/// </summary>
public sealed record PlanRevisionChange(
    PlanRevisionField Field,
    int? Order = null,
    DecisionPlanActionKind? BeforeAction = null,
    DecisionPlanActionKind? AfterAction = null,
    PayoffRolloverKind? BeforeKind = null,
    PayoffRolloverKind? AfterKind = null,
    PayoffPriorityChoice? BeforePriority = null,
    PayoffPriorityChoice? AfterPriority = null,
    PayoffConstraintKind? BeforeConstraint = null,
    PayoffConstraintKind? AfterConstraint = null,
    Guid? BeforeDebtId = null,
    Guid? AfterDebtId = null,
    decimal? BeforeAmount = null,
    decimal? AfterAmount = null,
    decimal? BeforeExtra = null,
    decimal? AfterExtra = null,
    int? BeforeCount = null,
    int? AfterCount = null,
    DateOnly? BeforeOn = null,
    DateOnly? AfterOn = null,
    string? BeforeName = null,
    string? AfterName = null,
    string? BeforeNextName = null,
    string? AfterNextName = null,
    bool? BeforeAlreadyShort = null,
    bool? AfterAlreadyShort = null);
