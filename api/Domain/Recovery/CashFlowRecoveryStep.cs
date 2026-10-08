namespace Cardui.Api.Domain.Recovery;

/// <summary>
/// One payoff that removes a monthly obligation.
/// EndedOn is the due date that brought the balance to zero. The minimum is still paid that month.
/// StartsOn is the next monthly due date, when the minimum is no longer paid.
/// It is null when that date cannot be represented or falls after the latest date the debt rules allow.
/// Minimum is the obligation removed. Extra is the planned extra on that debt, and Amount is those two added together.
/// A smaller final payment does not reduce Amount. BreathingRoom is the recurring freed cash after this step.
/// It does not include shared extra. BreathingRoomAdded is the increase from the previous step.
/// </summary>
public sealed record CashFlowRecoveryStep(
    Guid DebtId,
    string Name,
    DateOnly EndedOn,
    DateOnly? StartsOn,
    decimal Minimum,
    decimal Extra,
    decimal Amount,
    decimal BreathingRoomAdded,
    decimal BreathingRoom);
