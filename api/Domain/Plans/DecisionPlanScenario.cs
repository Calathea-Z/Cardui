namespace Cardui.Api.Domain.Plans;

/// <summary>
/// The cash preview selected for this plan.
/// A null spending or floor amount means the plan used the household's saved figure.
/// A windfall stays in cash. Income-loss months leave out included pay and do not move money.
/// </summary>
public sealed record DecisionPlanScenario(
    decimal MonthlyExtra,
    int IncomeLossMonths,
    decimal WindfallAmount,
    DateOnly? WindfallOn,
    decimal? LivingSpendingMonthly,
    decimal? CashFloor);
