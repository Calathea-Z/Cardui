namespace Cardui.Api.Domain.Recovery;

/// <summary>
/// The monthly cash a debt stops requiring once its modeled payment has ended.
/// EndedOn is the due date that brought the balance to zero. The cash is not free on that date.
/// Minimum is the planned minimum, and Extra is the extra that was planned for this debt.
/// Amount is those two added together. A last payment that was smaller, because the balance was smaller, does not reduce Amount.
/// </summary>
public sealed record PayoffFreedPayment(
    Guid DebtId,
    string Name,
    DateOnly EndedOn,
    decimal Minimum,
    decimal Extra,
    decimal Amount);
