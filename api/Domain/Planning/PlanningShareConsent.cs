namespace Cardui.Api.Domain.Planning;

/// <summary>
/// Whether this conversation may include household data in a planning-model request.
/// Not asked and declined both refuse a share. Allowed is an explicit yes for this conversation.
/// The member name is the JSON text. This value is not stored. The stored choice is the words allowed and declined.
/// </summary>
public enum PlanningShareConsent
{
    NotAsked,
    Allowed,
    Declined
}
