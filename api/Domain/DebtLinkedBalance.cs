namespace Cardui.Api.Domain;

/// <summary>
/// The latest dated balance of an account linked to a debt.
/// AsOf is null when the balance has no snapshot date. Currency is null when the account does not say.
/// IsLiability is true for a credit card or loan, the accounts whose balance is an amount owed.
/// </summary>
public readonly record struct DebtLinkedBalance(
    decimal Balance,
    DateOnly? AsOf,
    string? Currency,
    bool IsLiability);
