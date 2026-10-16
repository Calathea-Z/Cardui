namespace Cardui.Api.Dtos.Plan;

/// <summary>
/// Cash, budget, and payoff figures captured for one decision plan.
/// Living spending and the cash floor are the amounts that plan used. Zero means it had none.
/// A null date means that event does not happen in the calculation.
/// </summary>
public class DecisionPlanFactsDto
{
    public decimal StartingCash { get; set; }

    public decimal StartingReserve { get; set; }

    public decimal StartingAvailable { get; set; }

    public int StartingCashAccountCount { get; set; }

    public DateOnly? StartingCashOldestAsOf { get; set; }

    public int StaleCashCount { get; set; }

    public bool HasIncome { get; set; }

    public bool HasBills { get; set; }

    public decimal LivingSpendingMonthly { get; set; }

    public decimal CashFloor { get; set; }

    public bool HasEmergencyGoal { get; set; }

    public int NamedSavingsGoalCount { get; set; }

    public int StaleDebtCount { get; set; }

    public int PaymentReviewCount { get; set; }

    public decimal Spendable { get; set; }

    public bool AlreadyShort { get; set; }

    public required DecisionPlanGuardrailDto Guardrail { get; set; }

    public DateOnly? ShortfallOn { get; set; }

    public DateOnly? RecoveredOn { get; set; }

    public DateOnly? ReserveShortfallOn { get; set; }

    public decimal? StartingObligation { get; set; }

    public decimal RecurringRoom { get; set; }

    public DateOnly? PaidOffOn { get; set; }

    public decimal TotalInterest { get; set; }
}
