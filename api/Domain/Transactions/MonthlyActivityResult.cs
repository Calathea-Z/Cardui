namespace Cardui.Api.Domain.Transactions;

/// <summary>
/// Planning-currency income and spending, plus the transactions left out
/// because their currency does not match.
/// </summary>
internal sealed record MonthlyActivityResult(
    TransactionActivityTotals Totals,
    int ExcludedTransactionCount,
    IReadOnlyList<string> ExcludedCurrencies);
