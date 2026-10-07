namespace Cardui.Api.Domain.CategoryTargets;

/// <summary>
/// A spending category the household can give a monthly target.
/// Income and transfers are not in this list.
/// </summary>
internal sealed record ExpenseCategoryRow(
    Guid Id,
    string Name,
    string? Color,
    string? Icon,
    string SubGroupName,
    int SubGroupSortOrder,
    string GroupKey);
