namespace Cardui.Api.Domain.CategoryTargets;

/// <summary>
/// One started month of targets, including the month it was copied from when that happened.
/// </summary>
internal sealed record SavedTargetMonth(
    Guid Id,
    int Year,
    int Month,
    int? CopiedFromYear,
    int? CopiedFromMonth,
    IReadOnlyList<CategoryTargetAssignment> Assignments);
