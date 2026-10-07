namespace Cardui.Api.Domain.Transactions;

/// <summary>
/// Totals for one month, quarter, or year in a merchant's history.
/// </summary>
public readonly record struct MerchantHistoryPeriod(
    string Key,
    string Label,
    string ShortLabel,
    decimal TotalAmount,
    int TransactionCount);
