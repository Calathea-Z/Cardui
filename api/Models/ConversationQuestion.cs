namespace Cardui.Api.Models;

/// <summary>
/// A question the conversation still needs, or one that was answered.
/// AnsweredAt is null while the question is open. A resume lists only the open ones.
/// </summary>
public class ConversationQuestion
{
    /// <summary>
    /// The longest question, in characters.
    /// </summary>
    public const int TextMaxLength = 500;

    public Guid Id { get; set; }

    public Guid ConversationId { get; set; }

    public Conversation? Conversation { get; set; }

    public required string Text { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? AnsweredAt { get; set; }
}
