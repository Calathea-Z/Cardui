namespace Cardui.Api.Domain.Plans;

/// <summary>
/// Whether the extra payment in this plan leaves ordinary spending or a near-term bill or debt payment short.
/// Active is false when no extra is tried or the extra does not make the 30-day horizon worse.
/// Shortfall is how far below zero that day falls. It is zero when the guardrail is inactive.
/// </summary>
public sealed record DecisionPlanGuardrail(
    bool Active,
    bool OrdinaryExpenses,
    bool NearTermObligations,
    DateOnly? On,
    decimal Shortfall,
    bool AlreadyShortWithoutExtra);
