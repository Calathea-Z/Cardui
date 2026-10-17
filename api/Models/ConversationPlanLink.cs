namespace Cardui.Api.Models;

/// <summary>
/// A saved plan version this conversation referred to.
/// The link does not accept, copy, or edit that version. The version must belong to the same household.
/// </summary>
public class ConversationPlanLink
{
    public Guid ConversationId { get; set; }

    public Conversation? Conversation { get; set; }

    public Guid PlanVersionId { get; set; }

    public PlanVersion? PlanVersion { get; set; }

    public DateTimeOffset LinkedAt { get; set; }
}
