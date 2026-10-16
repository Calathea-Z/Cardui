namespace Cardui.Api.Dtos.Plan;

/// <summary>
/// Whether the tried extra payment leaves ordinary spending or near-term bills and debt payments short.
/// Active is false when no extra is tried or the extra does not make the horizon worse.
/// On is null when the guardrail is inactive. Shortfall is how far below zero that day falls.
/// </summary>
public class SpendableGuardrailDto
{
    public bool Active { get; set; }

    public bool OrdinaryExpenses { get; set; }

    public bool NearTermObligations { get; set; }

    public DateOnly? On { get; set; }

    public decimal Shortfall { get; set; }

    public bool AlreadyShortWithoutExtra { get; set; }
}
