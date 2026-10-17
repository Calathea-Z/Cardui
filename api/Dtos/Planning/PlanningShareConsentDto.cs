using Cardui.Api.Domain.Planning;

namespace Cardui.Api.Dtos.Planning;

/// <summary>
/// The sharing choice stored on one conversation.
/// NotAsked means no choice is stored. A closed conversation can still be read.
/// </summary>
public sealed class PlanningShareConsentDto
{
    public Guid ConversationId { get; init; }

    public PlanningShareConsent Consent { get; init; }
}
