namespace Cardui.Api.Domain.Debts;

/// <summary>
/// A debt field the person can keep as their own while following an account.
/// Balance follows the latest snapshot. Credit limit follows the account limit on a revolving debt.
/// </summary>
public enum DebtSyncedField
{
    Balance,
    CreditLimit
}
