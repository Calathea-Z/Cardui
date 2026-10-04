namespace Cardui.Api.Domain;

/// <summary>
/// One day's carried-forward totals for the balance chart.
/// </summary>
public readonly record struct AccountBalanceHistoryPoint(
    DateOnly Date,
    decimal Cash,
    decimal Investments,
    decimal CreditCards,
    decimal Loans,
    decimal NetWorth);
