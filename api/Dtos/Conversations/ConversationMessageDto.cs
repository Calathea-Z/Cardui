using Cardui.Api.Models;

namespace Cardui.Api.Dtos.Conversations;

/// <summary>
/// One stored turn. Sequence starts at 1 and is the order to resume in.
/// </summary>
public sealed class ConversationMessageDto
{
    public Guid Id { get; set; }

    public int Sequence { get; set; }

    public ConversationMessageRole Role { get; set; }

    public required string Text { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
