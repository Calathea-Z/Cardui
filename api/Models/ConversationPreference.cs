namespace Cardui.Api.Models;

/// <summary>
/// A short note the conversation should remember, such as a preference the person stated.
/// The same name, ignoring case, is one note. Saving a note does not change allocation, pay, bills, debts, or a saved plan.
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
