namespace Cardui.Api.Dtos.Plan;

public class PlanRecoveryDto
{
    public required string PlanningCurrency { get; set; }

    public required PlanRecoveryPathDto Rollover { get; set; }

    /// <summary>
    /// Every freed payment is kept instead of paying the next debt.
    /// </summary>
    public required PlanRecoveryPathDto ReclaimAll { get; set; }

    public IReadOnlyList<string> ExcludedCurrencies { get; set; } = [];

    /// <summary>
    /// Debts left out of both paths because their balance is unknown.
    /// </summary>
    public IReadOnlyList<PlanMissingBalanceDto> MissingBalance { get; set; } = [];

    /// <summary>
    /// True when the household has at least one debt.
    /// A debt with no balance is still counted. It is left out of the payoff, because zero would mean already paid off.
    /// </summary>
    public bool HasDebts { get; set; }

    /// <summary>
    /// Cash over 30 days and 6, 12, and 18 months, with debt payments from each path.
    /// </summary>
    public required PlanCashOutlookDto CashOutlook { get; set; }

    /// <summary>
    /// Shared extra tried for this response, in the planning currency.
    /// Zero is minimums only. The amount is not saved.
    /// </summary>
    public decimal MonthlyExtra { get; set; }

    /// <summary>
    /// Monthly living spending in the planning currency.
    /// Zero means no flexible living-spending amount is recorded.
    /// </summary>
    public decimal LivingSpendingMonthly { get; set; }

    /// <summary>
    /// Whole months of included pay left out of this preview, starting today. Zero keeps every paycheck.
    /// </summary>
    public int IncomeLossMonths { get; set; }

    /// <summary>
    /// One-time cash added on <see cref="WindfallOn"/>. Zero means this preview has no windfall.
    /// </summary>
    public decimal WindfallAmount { get; set; }

    /// <summary>
    /// The windfall date. Null when this preview has no windfall.
    /// </summary>
    public DateOnly? WindfallOn { get; set; }

    /// <summary>
    /// Flexible spending used instead of <see cref="LivingSpendingMonthly"/>. Null keeps the saved amount.
    /// </summary>
    public decimal? ScenarioLivingSpendingMonthly { get; set; }

    /// <summary>
    /// The saved cash-to-keep amount in the planning currency. Zero means no floor is saved.
    /// </summary>
    public decimal CashFloor { get; set; }

    /// <summary>
    /// Cash to keep used instead of <see cref="CashFloor"/>. Null keeps the saved floor.
    /// </summary>
    public decimal? ScenarioCashFloor { get; set; }

    /// <summary>
    /// Balance sources and dates for every saved debt, including zero-balance debts excluded from payoff calculations.
    /// </summary>
    public IReadOnlyList<PlanDebtFactDto> DebtFacts { get; set; } = [];

    public bool HasCashFloor { get; set; }

    public bool HasEmergencyGoal { get; set; }

    public int NamedSavingsGoalCount { get; set; }
}
