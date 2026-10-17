namespace Cardui.Api.Domain.Planning;

/// <summary>
/// The versions a proposal was reviewed against.
/// ConversationUpdatedAtUtcTicks is the open conversation's update time.
/// OpenDraftId and AcceptedPlanId are null when the household has no row in that status.
/// PlanFingerprint is the calculated decision plan. A later edit that changes the plan changes the fingerprint.
/// </summary>
public sealed record PlanRevisionContext(
    Guid ConversationId,
    long ConversationUpdatedAtUtcTicks,
    Guid? OpenDraftId,
    Guid? AcceptedPlanId,
    string PlanFingerprint);
