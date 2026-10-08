namespace Cardui.Api.Domain.Recovery;

/// <summary>
/// The monthly cash a debt stops requiring once its modeled payment has ended.
/// EndedOn is the due date that brought the balance to zero. The cash is not free on that date.
/// StartsOn is the next monthly due date, when the minimum is no longer paid.
/// It is null when that date cannot be represented or falls after the latest date the debt rules allow.
/// Minimum is the planned minimum, and Extra is the extra that was planned for this debt.
/// Amount is those two added together. A last payment that was smaller, because the balance was smaller, does not reduce Amount.
/// </summary>
public sealed record PayoffFreedPayment(
    Guid DebtId,
    string Name,
    DateOnly EndedOn,
    DateOnly? StartsOn,
    decimal Minimum,
    decimal Extra,
    decimal Amount);
