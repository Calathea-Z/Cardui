namespace Cardui.Api.Domain;

/// <summary>
/// A debt field the person can keep as their own while following an account.
/// Balance is the only followed field so far. Credit limit joins when that value is stored.
/// </summary>
public enum DebtSyncedField
{
    Balance
}
