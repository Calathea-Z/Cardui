namespace Cardui.Api.Models;

/// <summary>
/// A short note the conversation should remember, or the data-sharing choice for this conversation.
/// The same name, ignoring case, is one row. A note does not change allocation, pay, bills, debts, or a saved plan.
/// The data-sharing name is not a note. Only the sharing methods can set it.
/// </summary>
public class ConversationPreference
{
    /// <summary>
    /// The longest note name, in characters.
    /// </summary>
    public const int KeyMaxLength = 80;

    /// <summary>
    /// The longest note, in characters.
    /// </summary>
    public const int ValueMaxLength = 500;

    public Guid Id { get; set; }

    public Guid ConversationId { get; set; }

    public Conversation? Conversation { get; set; }

    public required string Key { get; set; }

    public required string Value { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
