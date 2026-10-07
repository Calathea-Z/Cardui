namespace Cardui.Api.Domain.Accounts;

/// <summary>
/// An account balance and its currency, used to decide whether the balance
/// counts in the planning currency.
/// </summary>
internal readonly record struct AccountCurrencyBalance(
    string Type,
    decimal CurrentBalance,
    string? CurrencyCode);
