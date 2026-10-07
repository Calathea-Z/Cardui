namespace Cardui.Api.Domain.Transactions;

/// <summary>
/// Spending total for one category in a date range.
/// </summary>
public sealed record TransactionActivityCategoryTotal(
    Guid? CategoryId,
    string CategoryName,
    string? CategoryColor,
    decimal Amount);
