namespace Cardui.Api.Domain.Planning;

/// <summary>
/// Why a planning-model call stopped and left the draft unchanged.
/// The member name is the log text. This value is not stored.
/// </summary>
public enum PlanningCallFallback
{
    CharacterCap,
    NotConfigured,
    NoAnswer
}
