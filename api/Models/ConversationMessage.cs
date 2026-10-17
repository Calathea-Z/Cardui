namespace Cardui.Api.Models;

/// <summary>
/// One turn in a conversation, in the order it was stored.
/// Sequence starts at 1. The text is what was said. This row is not a financial fact.
/// </summary>
public class ConversationMessage
{
    /// <summary>
    /// The longest turn, in characters.
    /// </summary>
    public const int TextMaxLength = 8000;

    public Guid Id { get; set; }

    public Guid ConversationId { get; set; }

    public Conversation? Conversation { get; set; }

    public int Sequence { get; set; }

    public ConversationMessageRole Role { get; set; }

    public required string Text { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
