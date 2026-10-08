namespace Cardui.Api.Domain.Recovery;

/// <summary>
/// One debt at a horizon.
/// Balance is what is still owed after payments due on or before that horizon.
/// MonthlyMinimum is the amount still due each month. It is zero when the debt is paid off,
/// and null when the rate, minimum, or due date is unknown.
/// PaidOffOn is the due date that cleared the balance, when that date is on or before the horizon.
/// </summary>
public sealed record ForecastDebtBalance(
    Guid DebtId,
    string Name,
    decimal Balance,
    decimal? MonthlyMinimum,
    DebtScheduleStop Stop,
    DateOnly? PaidOffOn);
