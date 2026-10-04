namespace Cardui.Api.Domain;

/// <summary>
/// Totals for one month, quarter, or year in a merchant's history.
/// </summary>
public readonly record struct MerchantHistoryPeriod(
    string Key,
    string Label,
    string ShortLabel,
    decimal TotalAmount,
    int TransactionCount);
