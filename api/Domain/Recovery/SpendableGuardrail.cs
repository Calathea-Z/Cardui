namespace Cardui.Api.Domain.Recovery;

/// <summary>
/// Whether an extra payment leaves ordinary spending or near-term bills and debt payments short.
/// Active is false when no extra is tried, or the extra does not make a shortfall worse inside the horizon.
/// On is the first date that shortfall shows. Shortfall is how far cash after set-aside money is below zero that day.
/// AlreadyShortWithoutExtra is true when that date is already short on minimums alone.
/// </summary>
public sealed record SpendableGuardrail(
    bool Active,
    bool OrdinaryExpenses,
    bool NearTermObligations,
    DateOnly? On,
    decimal Shortfall,
    bool AlreadyShortWithoutExtra);
