namespace Cardui.Api.Models;

/// <summary>
/// Whether a household can still add to a conversation.
/// A household has at most one open conversation. Closed conversations stay readable.
/// The stored column and the API use the member name.
/// </summary>
public enum ConversationStatus
{
    Open,
    Closed
}
