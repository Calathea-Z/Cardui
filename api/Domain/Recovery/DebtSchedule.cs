namespace Cardui.Api.Domain.Recovery;

/// <summary>
/// The monthly projection for one debt.
/// EndingBalance is what remains after the last period. When no period is produced, it is the starting balance,
/// or zero when that balance was already paid off. Stop says why the projection ended.
/// Payments are monthly from the due date. A freed minimum is not rolled onto another debt.
/// </summary>
public sealed record DebtSchedule(
    Guid DebtId,
    string Name,
    string Currency,
    DebtScheduleStop Stop,
    decimal EndingBalance,
    IReadOnlyList<DebtPeriod> Periods);
