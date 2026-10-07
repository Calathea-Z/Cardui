namespace Cardui.Api.Domain.Debts;

/// <summary>
/// Where the balance shown for a debt comes from.
/// Synced uses the connected account. Override uses a value the person set while following.
/// Manual uses the debt's own balance, including when the connection has nothing usable.
/// </summary>
public enum DebtFieldSource
{
    Synced,
    Override,
    Manual
}
