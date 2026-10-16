namespace Cardui.Api.Domain.Planning;

/// <summary>
/// One debt the question rule can ask about.
/// A missing minimum or due date is a cash question. A missing rate waits until that payment is known or left unknown.
/// DueDatePassed is true when the saved due date is before today. A missing date is not passed. A known zero minimum is not missing.
/// </summary>
public sealed record PlanningDebtQuestionFact(
    Guid DebtId,
    string Name,
    bool MissingMinimum,
    bool MissingDueDate,
    bool MissingApr,
    DateOnly? NextDueDate,
    bool DueDatePassed);
