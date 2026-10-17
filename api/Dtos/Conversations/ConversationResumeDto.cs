using Cardui.Api.Models;

namespace Cardui.Api.Dtos.Conversations;

/// <summary>
/// A conversation as it can be resumed on a later day.
/// Summary is null until one is saved. Questions lists only those still unanswered.
/// Preferences are notes on this conversation, plus the data-sharing choice when one is stored.
/// A note does not change pay, bills, debts, allocation, or a saved plan.
/// Plan versions are links. A link does not accept or edit the plan.
/// </summary>
public sealed class ConversationResumeDto
{
    public Guid Id { get; set; }

    public ConversationStatus Status { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public DateTimeOffset? ClosedAt { get; set; }

    public string? Summary { get; set; }

    public List<ConversationMessageDto> Messages { get; set; } = [];

    public List<ConversationQuestionDto> Questions { get; set; } = [];

    public List<ConversationPreferenceDto> Preferences { get; set; } = [];

    public List<ConversationPlanLinkDto> PlanVersions { get; set; } = [];
}
