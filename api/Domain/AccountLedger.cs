namespace Cardui.Api.Domain;

public readonly record struct LedgerTransaction(
    DateOnly Date,
    decimal Amount,
    bool Pending,
    bool Archived);

public static class AccountLedger
{
    public static bool IsLiability(string accountType) =>
        AccountTypes.IsCreditCard(accountType) || AccountTypes.IsLoan(accountType);

    public static decimal Round(decimal amount) =>
        decimal.Round(amount, 2, MidpointRounding.AwayFromZero);

    public static decimal BalanceAsOf(
        string accountType,
        decimal openingBalance,
        DateOnly openingBalanceDate,
        IEnumerable<LedgerTransaction> transactions,
        DateOnly asOf)
    {
        var balance = Round(openingBalance);

        foreach (var transaction in transactions)
        {
            if (transaction.Pending || transaction.Archived)
            {
                continue;
            }

            if (transaction.Date < openingBalanceDate || transaction.Date > asOf)
            {
                continue;
            }

            balance = Round(balance + BalanceEffect(accountType, transaction.Amount));
        }

        return balance;
    }

    public static decimal BalanceEffect(string accountType, decimal amount) =>
        IsLiability(accountType) ? amount : -amount;

    public static decimal TransactionAmountForBalanceChange(
        string accountType,
        decimal balanceChange) =>
        IsLiability(accountType) ? balanceChange : -balanceChange;
}
