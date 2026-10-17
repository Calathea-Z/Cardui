using Cardui.Api.Domain.Recovery;

namespace Cardui.Api.Domain.Planning;

/// <summary>
/// The labels around a calculated forecast.
/// PlanningCurrency is the code the calculation counted. HasDebts is true when a debt is recorded, including one with no balance.
/// MissingBalanceNames are left out of the payoff. They are not given a zero balance here.
/// </summary>
public sealed record PlanningForecastContext(
    string PlanningCurrency,
    PayoffPriorityChoice Priority,
    bool HasDebts,
    IReadOnlyList<string> MissingBalanceNames,
    IReadOnlyList<string> ExcludedCurrencies,
    IReadOnlyList<PlanningForecastGuardrail> Guardrails,
    IReadOnlyList<PlanningForecastPriority> Priorities,
    IReadOnlyList<PlanningForecastUncertainty> Uncertainties);
