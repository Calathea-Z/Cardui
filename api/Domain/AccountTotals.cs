namespace Cardui.Api.Domain;

public readonly record struct AccountTotals(
    decimal Cash,
    decimal Investments,
    decimal CreditCards,
    decimal Loans)
{
    public decimal Assets => Cash + Investments;
    public decimal Liabilities => CreditCards + Loans;
    public decimal NetWorth => Assets - Liabilities;
}
