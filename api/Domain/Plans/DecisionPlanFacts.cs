namespace Cardui.Api.Domain.Plans;

/// <summary>
/// The cash, budget, and payoff figures captured for one plan version.
/// Living spending and the cash floor are the amounts this plan used, including a scenario override. Zero means this plan had none.
/// Spendable is the 30-day figure, never a monthly surplus, and never negative.
/// LowestAvailable is that same lowest balance before it is floored, and it stays negative when the window is short.
/// LowestAvailableOn is the date of that low. Both are null on a version saved before they were stored.
/// ShortfallOn is the typical-pay cash date, not LowestAvailableOn. Other dates are null when that event does not happen in the calculation.
/// </summary>
public sealed record DecisionPlanFacts(
    decimal StartingCash,
    decimal StartingReserve,
    decimal StartingAvailable,
    int StartingCashAccountCount,
    DateOnly? StartingCashOldestAsOf,
    int StaleCashCount,
    bool HasIncome,
    bool HasBills,
    decimal LivingSpendingMonthly,
    decimal CashFloor,
    bool HasEmergencyGoal,
    int NamedSavingsGoalCount,
    int StaleDebtCount,
    int PaymentReviewCount,
    decimal Spendable,
    bool AlreadyShort,
    DecisionPlanGuardrail Guardrail,
    DateOnly? ShortfallOn,
    DateOnly? RecoveredOn,
    DateOnly? ReserveShortfallOn,
    decimal? StartingObligation,
    decimal RecurringRoom,
    DateOnly? PaidOffOn,
    decimal TotalInterest,
    decimal? LowestAvailable = null,
    DateOnly? LowestAvailableOn = null);
