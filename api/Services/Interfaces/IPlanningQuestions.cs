using Cardui.Api.Dtos.Conversations;
using Cardui.Api.Dtos.Planning;

namespace Cardui.Api.Services.Interfaces;

public interface IPlanningQuestions
{
    /// <summary>
    /// Chooses the next one or two questions from the household's cash, pay, bills, debts, and living spending.
    /// Urgent cash comes first. A Question: note whose value is unknown is skipped, and that detail is not treated as zero.
    /// Nothing is saved. Another household is not read.
    /// </summary>
    Task<IReadOnlyList<PlanningQuestionDto>> SelectAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores a note that the person does not know this detail.
    /// The note name is Question: followed by the question key, and the value is unknown.
    /// A note the person wrote under another name is left alone.
    /// Pay, bills, debts, cash, and living spending stay as they are.
    /// A closed conversation is rejected. A debt from another household is not found.
    /// </summary>
    Task<ConversationResumeDto> LeaveUnknownAsync(
        Guid conversationId,
        string key,
        CancellationToken cancellationToken = default);
}
