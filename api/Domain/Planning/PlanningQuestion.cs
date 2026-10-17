namespace Cardui.Api.Domain.Planning;

/// <summary>
/// One question to ask next, and why the detail matters.
/// DebtId is set when the question is about one debt. Key is the note name used when the person does not know.
/// Every question here can be left unknown. Unknown does not mean zero.
/// </summary>
public sealed record PlanningQuestion(
    PlanningQuestionTopic Topic,
    string Key,
    Guid? DebtId,
    string Text,
    string Reason);
