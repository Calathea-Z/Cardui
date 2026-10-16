using Cardui.Api.Domain.Planning;

namespace Cardui.Api.Dtos.Planning;

/// <summary>
/// The next question to ask, and why that detail changes the cash picture.
/// DebtId is set when the question is about one debt. Key is the note name used when the person does not know.
/// Unknown is allowed. It does not mean zero, and it does not save the fact.
/// </summary>
public sealed class PlanningQuestionDto
{
    public PlanningQuestionTopic Topic { get; init; }

    public required string Key { get; init; }

    public Guid? DebtId { get; init; }

    public required string Text { get; init; }

    public required string Reason { get; init; }
}
