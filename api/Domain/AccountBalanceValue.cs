namespace Cardui.Api.Domain;

public readonly record struct AccountBalanceValue(
    string Type,
    decimal CurrentBalance);
