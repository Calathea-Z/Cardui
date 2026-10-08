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
}
