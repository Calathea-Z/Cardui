namespace Cardui.Api.Dtos.Conversations;

/// <summary>
/// One question that is still unanswered.
/// </summary>
public sealed class ConversationQuestionDto
{
    public Guid Id { get; set; }

    public required string Text { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
