namespace Cardui.Api.Domain.Planning;

/// <summary>
/// One part of a plan revision the person can review.
/// The member name is the JSON text. This value is not stored.
/// An action is one ranked step. Starting cash, living spending, and the cash floor are amounts the calculation used.
/// CalculatedPlan means the documents differ and the choice, scenario, actions, and those amounts do not. It carries no amount.
/// </summary>
public enum PlanRevisionField
{
    FreedCash,
    MonthlyExtra,
    IncomeLoss,
    Windfall,
    WindfallDate,
    FlexibleSpending,
    CashToKeep,
    PayoffPriority,
    Constraint,
    Reclaim,
    Action,
    StartingCash,
    LivingSpending,
    CashFloor,
    CalculatedPlan
}
