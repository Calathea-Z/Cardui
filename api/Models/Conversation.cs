namespace Cardui.Api.Models;

/// <summary>
/// One planning conversation for a household.
/// The household has at most one open conversation. Messages, the compact summary,
/// unanswered questions, notes, and linked plan versions stay on it so a later day can resume.
/// Deleting the household deletes the conversation. This row does not change pay, bills, debts, or a saved plan.
/// </summary>
public class Conversation
{
    /// <summary>
    /// The longest compact summary, in characters. The messages stay either way.
    /// </summary>
    public const int SummaryMaxLength = 4000;

    /// <summary>
    /// Allows one open conversation per household. Closed conversations are not in this index.
    /// </summary>
    public const string OpenHouseholdIndexName = "IX_Conversations_HouseholdId_Open";

    public Guid Id { get; set; }

    public Guid HouseholdId { get; set; }

    public Household? Household { get; set; }

    public ConversationStatus Status { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public DateTimeOffset? ClosedAt { get; set; }

    /// <summary>
    /// A short stand-in for the thread. Null until one is saved.
    /// Replacing it does not remove the messages.
    /// </summary>
    public string? Summary { get; set; }

    public ICollection<ConversationMessage> Messages { get; init; } =
        new List<ConversationMessage>();

    public ICollection<ConversationQuestion> Questions { get; init; } =
        new List<ConversationQuestion>();

    public ICollection<ConversationPreference> Preferences { get; init; } =
        new List<ConversationPreference>();

    public ICollection<ConversationPlanLink> PlanLinks { get; init; } =
        new List<ConversationPlanLink>();
}
