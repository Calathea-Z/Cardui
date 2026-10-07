namespace Cardui.Api.Domain;

/// <summary>
/// Posted spending for one category in one month.
/// Spent is the non-negative category total from the activity calculator.
/// A null category is uncategorized spending.
/// </summary>
public sealed record CategoryMonthSpent(
    Guid? CategoryId,
    decimal Spent);
