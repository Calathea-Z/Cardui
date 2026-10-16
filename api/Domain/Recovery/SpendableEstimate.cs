namespace Cardui.Api.Domain.Recovery;

/// <summary>
/// How much cash can be used inside one horizon without spending money set aside.
/// Spendable is the lowest cash left after the reserve, and it is never negative. A shortfall stays on LowestAvailable.
/// ReliableIncome, Obligations, and OrdinarySpending are the dated totals inside the horizon, not a monthly average.
/// AlreadyShort is true when that lowest day is below zero before any extra payment.
/// </summary>
public sealed record SpendableEstimate(
    DateOnly From,
    DateOnly Through,
    decimal Spendable,
    decimal LowestAvailable,
    DateOnly LowestOn,
    decimal ReliableIncome,
    decimal Obligations,
    decimal OrdinarySpending,
    decimal Reserved,
    bool AlreadyShort,
    SpendableGuardrail Guardrail,
    IReadOnlyList<SpendableUncertainty> Uncertainties);
