namespace Cardui.Api.Dtos.Plan;

public class CashFlowRecoveryReportDto
{
    public required string PlanningCurrency { get; set; }

    /// <summary>
    /// Shared extra directed at the first debt that can take it.
    /// Zero until a saved extra exists.
    /// </summary>
    public decimal MonthlyExtra { get; set; }

    /// <summary>
    /// Freed cash kept each month on the partial reclaim path.
    /// Zero until a saved reclaim amount exists, so that path matches rollover.
    /// </summary>
    public decimal ReclaimAmount { get; set; }

    public required CashFlowRecoveryPathDto Rollover { get; set; }

    public required CashFlowRecoveryPathDto Reclaim { get; set; }

    public required CashFlowRecoveryPathDto ReclaimAll { get; set; }

    public IReadOnlyList<string> ExcludedCurrencies { get; set; } = [];

    public IReadOnlyList<string> Assumptions { get; set; } = [];

    /// <summary>
    /// True when the household has at least one debt.
    /// A debt with no balance is still counted. It is left out of the payoff, because zero would mean already paid off.
    /// </summary>
    public bool HasDebts { get; set; }
}
