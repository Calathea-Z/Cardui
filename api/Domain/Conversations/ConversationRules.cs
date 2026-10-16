using Cardui.Api.Models;

namespace Cardui.Api.Domain.Conversations;

/// <summary>
/// Checks text and state before a conversation is stored.
/// The database still enforces one open conversation per household.
/// </summary>
public static class ConversationRules
{
    /// <summary>
    /// Rejects blank text and text longer than the stored limit.
    /// Spaces at the ends do not count toward the limit. A value that is only spaces is blank.
    /// </summary>
    public static string? RejectText(string? value, int maxLength, string noun)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return $"Enter a {noun}.";
        }

        if (value.Trim().Length > maxLength)
        {
            return $"A {noun} must be {maxLength} characters or fewer.";
        }

        return null;
    }

    /// <summary>
    /// Rejects a role this conversation cannot store.
    /// A turn is from the person or the assistant.
    /// </summary>
    public static string? RejectRole(ConversationMessageRole role)
    {
        return role is ConversationMessageRole.User or ConversationMessageRole.Assistant
            ? null
            : "A message is from the person or the assistant.";
    }

    /// <summary>
    /// Rejects a change to a conversation that is already closed.
    /// A closed conversation stays readable.
    /// </summary>
    public static string? RejectClosed(ConversationStatus status)
    {
        return status == ConversationStatus.Open
            ? null
            : "This conversation is closed.";
    }

    /// <summary>
    /// Removes spaces at the ends. The caller has already rejected a blank value.
    /// </summary>
    public static string Normalize(string value)
    {
        return value.Trim();
    }

    /// <summary>
    /// True when both values are the same note or the same question, ignoring case and end spaces.
    /// </summary>
    public static bool SameText(string left, string right)
    {
        return string.Equals(
            left.Trim(),
            right.Trim(),
            StringComparison.OrdinalIgnoreCase);
    }
}
