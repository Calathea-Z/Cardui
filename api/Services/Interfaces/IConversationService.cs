using Cardui.Api.Dtos.Conversations;
using Cardui.Api.Models;

namespace Cardui.Api.Services.Interfaces;

public interface IConversationService
{
    /// <summary>
    /// Returns the household's open conversation, or null when none is open.
    /// A closed conversation is not returned here. The saved turns stay until a later change.
    /// </summary>
    Task<ConversationResumeDto?> GetOpenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns one conversation for this household, including one that is closed.
    /// The turns, summary, unanswered questions, notes, and plan links are the ones last saved.
    /// </summary>
    Task<ConversationResumeDto> GetAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the household's open conversation.
    /// When none is open, starts a new one. A new conversation does not copy an earlier summary, questions, or notes.
    /// </summary>
    Task<ConversationResumeDto> StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Closes the conversation. It stays readable, and a later start begins a new one.
    /// Closing it again returns it unchanged.
    /// </summary>
    Task<ConversationResumeDto> CloseAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores one turn from the person or the assistant.
    /// This does not call the planning model. Sequence continues from the last stored turn.
    /// </summary>
    Task<ConversationResumeDto> AppendMessageAsync(
        Guid conversationId,
        ConversationMessageRole role,
        string text,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces the compact summary. Null clears it. The messages stay.
    /// The same summary does not change the conversation. This does not write a summary from a model.
    /// </summary>
    Task<ConversationResumeDto> ReplaceSummaryAsync(
        Guid conversationId,
        string? summary,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds an unanswered question.
    /// The same question, ignoring case, stays the one already open.
    /// </summary>
    Task<ConversationResumeDto> AddQuestionAsync(
        Guid conversationId,
        string text,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a question answered. An answered question is left out when the conversation is read.
    /// Answering it again returns the conversation unchanged.
    /// </summary>
    Task<ConversationResumeDto> AnswerQuestionAsync(
        Guid conversationId,
        Guid questionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a note on this conversation.
    /// The same name, ignoring case, replaces the note and keeps the name that was stored first.
    /// Pay, bills, debts, allocation, and saved plans stay as they are.
    /// </summary>
    Task<ConversationResumeDto> SetPreferenceAsync(
        Guid conversationId,
        string key,
        string value,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes one note. The name match ignores case.
    /// </summary>
    Task<ConversationResumeDto> RemovePreferenceAsync(
        Guid conversationId,
        string key,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Links a saved plan version from this household.
    /// The version is not accepted, copied, or edited. Linking it again returns the conversation unchanged.
    /// </summary>
    Task<ConversationResumeDto> LinkPlanVersionAsync(
        Guid conversationId,
        Guid planVersionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a plan link. The saved plan stays.
    /// </summary>
    Task<ConversationResumeDto> UnlinkPlanVersionAsync(
        Guid conversationId,
        Guid planVersionId,
        CancellationToken cancellationToken = default);
}
