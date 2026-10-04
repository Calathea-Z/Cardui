namespace Cardui.Api.Domain;

public static class AccountLedger
{
    /// <summary>
    /// True for a credit card or loan. Those balances are amounts owed.
    /// </summary>
    public static bool IsLiability(string accountType) =>
        AccountTypes.IsCreditCard(accountType) || AccountTypes.IsLoan(accountType);

    /// <summary>
    /// Rounds money to cents, away from zero.
    /// </summary>
    public static decimal Round(decimal amount) =>
        decimal.Round(amount, 2, MidpointRounding.AwayFromZero);

    /// <summary>
    /// Calculates the balance on a date from the opening balance and posted
    /// transactions. Pending and archived transactions are ignored, as are
    /// transactions outside the opening date and the as-of date.
    /// Cash and investments fall when money leaves. Credit cards and loans
    /// rise when money is borrowed.
    /// </summary>
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

    /// <summary>
    /// How a transaction amount changes the balance. Liabilities use the
    /// amount as stored. Assets use the opposite sign.
    /// </summary>
    public static decimal BalanceEffect(string accountType, decimal amount) =>
        IsLiability(accountType) ? amount : -amount;

    /// <summary>
    /// Converts a desired balance change into the transaction amount to store.
    /// </summary>
    public static decimal TransactionAmountForBalanceChange(
        string accountType,
        decimal balanceChange) =>
        IsLiability(accountType) ? balanceChange : -balanceChange;
}
