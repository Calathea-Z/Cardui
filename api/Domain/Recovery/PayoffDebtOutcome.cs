namespace Cardui.Api.Domain.Recovery;

/// <summary>
/// How one debt fares in one payoff order.
/// Stop says why its payments ended. EndingBalance is what remains, including a balance that a payment did not reduce.
/// PaidOffOn is the due date that cleared it, and it is null when the balance remains.
/// PaymentsUntilPaidOff counts monthly payments from the first due date, and it is null when the debt is not paid off.
/// PaymentsUntilUnderLimit counts payments until a revolving debt that started at or above 90 percent is under that line.
/// It is null when the debt did not start that high or never crosses. Utilization is the opening ratio for a revolving debt.
/// Interest is the sum of the monthly interest charged while this debt was projected.
/// </summary>
public sealed record PayoffDebtOutcome(
    Guid DebtId,
    string Name,
    DebtScheduleStop Stop,
    decimal? Apr,
    decimal Balance,
    decimal? Minimum,
    decimal? Utilization,
    decimal Interest,
    decimal EndingBalance,
    DateOnly? PaidOffOn,
    int? PaymentsUntilPaidOff,
    int? PaymentsUntilUnderLimit,
    decimal? EndingUtilization);
