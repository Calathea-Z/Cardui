using Cardui.Api.Domain.Recovery;

namespace Cardui.Api.Domain.Planning;

/// <summary>
/// Why an extra payment leaves the 30-day window short, for one freed-cash choice.
/// OrdinaryExpenses and NearTermObligations say which gap the calculation found. AlreadyShort means the window was short before the extra.
/// This is present only when that guardrail is active.
/// </summary>
public sealed record PlanningForecastGuardrail(
    PayoffRolloverKind Path,
    bool OrdinaryExpenses,
    bool NearTermObligations,
    bool AlreadyShort);
