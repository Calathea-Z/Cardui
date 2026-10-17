namespace Cardui.Api.Dtos.Conversations;

/// <summary>
/// One note saved on the conversation.
/// Key is the name that was stored first. A later save with the same name, ignoring case, keeps that name.
/// </summary>
public sealed class ConversationPreferenceDto
{
    public Guid Id { get; set; }

    public required string Key { get; set; }

    public required string Value { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
