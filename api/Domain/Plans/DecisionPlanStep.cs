namespace Cardui.Api.Domain.Plans;

/// <summary>
/// One modeled payoff captured for this plan.
/// EndedOn is the due date that brought the balance to zero. The minimum is still paid that month.
/// StartsOn is the later date the minimum is no longer paid. Null means that date is outside the projection.
/// BreathingRoom is the recurring freed cash after this step and does not include shared extra.
/// </summary>
public sealed record DecisionPlanStep(
    Guid DebtId,
    string Name,
    DateOnly EndedOn,
    DateOnly? StartsOn,
    decimal Minimum,
    decimal BreathingRoom);
