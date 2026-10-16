namespace Cardui.Api.Dtos.Plan;

/// <summary>
/// Whether the extra payment in a decision plan leaves ordinary spending or a near-term bill or debt payment short.
/// Shortfall is how far below zero that day falls. It is zero when the guardrail is inactive.
/// </summary>
public class DecisionPlanGuardrailDto
{
    public bool Active { get; set; }

    public bool OrdinaryExpenses { get; set; }

    public bool NearTermObligations { get; set; }

    public DateOnly? On { get; set; }

    public decimal Shortfall { get; set; }

    public bool AlreadyShortWithoutExtra { get; set; }
}
