namespace Cardui.Api.Domain.Planning;

/// <summary>
/// Whether one guarded planning-model call returned text or left the person on the manual plan.
/// Answered means the provider returned text. ContinueManually means the draft was left unchanged.
/// The member name is the JSON text. This value is not stored.
/// </summary>
public enum PlanningCallResult
{
    Answered,
    ContinueManually
}
