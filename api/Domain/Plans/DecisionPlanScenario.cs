using Cardui.Api.Domain.Recovery;

namespace Cardui.Api.Domain.Plans;

/// <summary>
/// The cash preview and payoff choice selected for this plan.
/// A null spending or floor amount means the plan used the household's saved figure.
/// A windfall stays in cash. Income-loss months leave out included pay and do not move money.
/// Priority is who extra and a rolled payment follow. A missing older document is avalanche.
/// ConstraintDebtId and ConstraintKind are set only when that priority is the user constraint.
/// ReclaimAmount is how much freed cash to keep each month when the plan keeps some. Zero keeps none.
/// </summary>
public sealed record DecisionPlanScenario(
    decimal MonthlyExtra,
    int IncomeLossMonths,
    decimal WindfallAmount,
    DateOnly? WindfallOn,
    decimal? LivingSpendingMonthly,
    decimal? CashFloor,
    PayoffPriorityChoice Priority = PayoffPriorityChoice.Avalanche,
    Guid? ConstraintDebtId = null,
    PayoffConstraintKind? ConstraintKind = null,
    decimal ReclaimAmount = 0);
