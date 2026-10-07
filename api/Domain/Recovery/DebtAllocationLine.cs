namespace Cardui.Api.Domain.Recovery;

/// <summary>
/// One debt in an extra-payment allocation.
/// Period is the payment it takes. Skip is set when it cannot take one, and that debt does not consume extra.
/// </summary>
public sealed record DebtAllocationLine(
    Guid DebtId,
    DebtPeriod? Period,
    DebtScheduleStop? Skip);
