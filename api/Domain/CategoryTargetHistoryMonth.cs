namespace Cardui.Api.Domain;

/// <summary>
/// An earlier month used to carry rollover forward.
/// An empty assignment list is a started month with no target for the categories being read.
/// A missing month is a gap and stops the carry.
/// </summary>
public sealed record CategoryTargetHistoryMonth(
    int Year,
    int Month,
    IReadOnlyList<CategoryTargetAssignment> Assignments,
    IReadOnlyList<CategoryMonthSpent> Spent);
