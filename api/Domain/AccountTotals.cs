namespace Cardui.Api.Domain;

/// <summary>
/// Cash, investment, credit card, and loan totals for a set of accounts.
/// </summary>
public readonly record struct AccountTotals(
    decimal Cash,
    decimal Investments,
    decimal CreditCards,
    decimal Loans)
{
    /// <summary>
    /// Cash plus investments.
    /// </summary>
    public decimal Assets => Cash + Investments;

    /// <summary>
    /// Credit cards plus loans, which are amounts owed.
    /// </summary>
    public decimal Liabilities => CreditCards + Loans;

    /// <summary>
    /// Assets minus liabilities.
    /// </summary>
    public decimal NetWorth => Assets - Liabilities;
}
