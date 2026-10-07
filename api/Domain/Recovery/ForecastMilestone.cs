namespace Cardui.Api.Domain.Recovery;

/// <summary>
/// One expected point on the forecast.
/// Amount is the cash balance, the available balance, or the monthly minimum that ends, depending on Kind.
/// SourceId is the debt when the point is a payoff. It is null for a cash or reserve point.
/// </summary>
public sealed record ForecastMilestone(
    DateOnly Date,
    ForecastMilestoneKind Kind,
    Guid? SourceId,
    string Name,
    decimal Amount);
