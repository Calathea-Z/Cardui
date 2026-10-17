namespace Cardui.Api.Domain.Planning;

/// <summary>
/// Which pay amounts a cash figure used.
/// LowPay is low pay where a source records it, without expected raises. Typical is every other figure, including payoff totals.
/// The member name is the text a later reader would store. This slice does not store it.
/// </summary>
public enum PlanningForecastBasis
{
    Typical,
    LowPay
}
