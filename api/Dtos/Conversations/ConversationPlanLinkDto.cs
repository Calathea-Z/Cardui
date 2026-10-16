using Cardui.Api.Models;

namespace Cardui.Api.Dtos.Conversations;

/// <summary>
/// A saved plan this conversation linked, as that version stands when it is read.
/// Number and Status come from the plan version. The link does not copy its document.
/// </summary>
public sealed class ConversationPlanLinkDto
{
    public Guid PlanVersionId { get; set; }

    public int Number { get; set; }

    public PlanVersionStatus Status { get; set; }

    public DateTimeOffset LinkedAt { get; set; }
}
