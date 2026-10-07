namespace Cardui.Api.Domain.Accounts;

/// <summary>
/// An account type and balance used when totaling net worth.
/// </summary>
public readonly record struct AccountBalanceValue(
    string Type,
    decimal CurrentBalance);
