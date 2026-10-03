namespace Cardui.Api.Domain;

public readonly record struct AccountBalanceHistoryPoint(
    DateOnly Date,
    decimal Cash,
    decimal Investments,
    decimal CreditCards,
    decimal Loans,
    decimal NetWorth);
