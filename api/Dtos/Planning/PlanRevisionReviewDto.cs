using Cardui.Api.Domain.Planning;

namespace Cardui.Api.Dtos.Planning;

/// <summary>
/// A proposed plan revision shown before it is accepted.
/// Applied is false. Changes are the differences from the accepted plan.
/// ReviewToken is the version check accept must send back. It is null when the review is stale.
/// ReplacesOpenDraft is true when accepting would replace an open draft that is not this proposal.
/// OpenDraftId and AcceptedPlanId are the current rows, and they are null when that row does not exist.
/// </summary>
public sealed class PlanRevisionReviewDto
{
    public bool Applied { get; init; }

    public bool Stale { get; init; }

    public PlanRevisionStaleReason? Reason { get; init; }

    public string? Message { get; init; }

    public Guid ConversationId { get; init; }

    public Guid? OpenDraftId { get; init; }

    public Guid? AcceptedPlanId { get; init; }

    public bool ReplacesOpenDraft { get; init; }

    public List<PlanRevisionChangeDto> Changes { get; init; } = [];

    public string? ReviewToken { get; init; }
}
