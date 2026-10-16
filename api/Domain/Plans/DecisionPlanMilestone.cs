namespace Cardui.Api.Domain.Plans;

/// <summary>
/// One milestone taken from the recovery calculation.
/// On is the date that event happens. Amount is a payment or freed cash when the milestone has one.
/// </summary>
public sealed record DecisionPlanMilestone(
    DecisionPlanMilestoneKind Kind,
    string? Name,
    decimal? Amount,
    DateOnly? On);
