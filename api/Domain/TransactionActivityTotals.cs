namespace Cardui.Api.Domain;

/// <summary>
/// Income, spending, and spending by category for a set of transactions.
/// </summary>
public sealed record TransactionActivityTotals(
    decimal Income,
    decimal Spending,
    IReadOnlyList<TransactionActivityCategoryTotal> SpendingByCategory);
