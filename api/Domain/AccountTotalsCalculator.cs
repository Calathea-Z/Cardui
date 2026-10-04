namespace Cardui.Api.Domain;

public static class AccountTotalsCalculator
{
    /// <summary>
    /// Sums balances into cash, investments, credit cards, and loans.
    /// Net worth is assets minus liabilities.
    /// </summary>
    public static AccountTotals Calculate(IEnumerable<AccountBalanceValue> accounts)
    {
        decimal cash = 0;
        decimal investments = 0;
        decimal creditCards = 0;
        decimal loans = 0;

        foreach (var account in accounts)
        {
            if (AccountTypes.IsCash(account.Type))
            {
                cash += account.CurrentBalance;
            }
            else if (AccountTypes.IsInvestment(account.Type))
            {
                investments += account.CurrentBalance;
            }
            else if (AccountTypes.IsCreditCard(account.Type))
            {
                creditCards += account.CurrentBalance;
            }
            else if (AccountTypes.IsLoan(account.Type))
            {
                loans += account.CurrentBalance;
            }
        }

        return new AccountTotals(cash, investments, creditCards, loans);
    }
}
