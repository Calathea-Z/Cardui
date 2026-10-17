using Cardui.Api.Domain.Plans;
using Cardui.Api.Models;

namespace Cardui.Api.Domain.Planning;

/// <summary>
/// The conversation and plan versions a review is compared with.
/// OpenDraft and AcceptedPlan are the stored documents. Null means that version is not open.
/// This value is not stored. A proposal is not a version until the person accepts it.
/// </summary>
public sealed record PlanRevisionBaseline(
    Guid ConversationId,
    ConversationStatus Status,
    DateTimeOffset UpdatedAt,
    Guid? OpenDraftId,
    DecisionPlan? OpenDraft,
    Guid? AcceptedPlanId,
    DecisionPlan? AcceptedPlan);
