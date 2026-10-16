namespace Cardui.Api.Domain.Plans;

/// <summary>
/// One assumption, risk, gap, or review condition.
/// Name, amount, date, and count are set only when that kind uses them. A missing value was not calculated.
/// </summary>
public sealed record DecisionPlanDetail(
    DecisionPlanDetailKind Kind,
    string? Name,
    decimal? Amount,
    DateOnly? On,
    int? Count);
