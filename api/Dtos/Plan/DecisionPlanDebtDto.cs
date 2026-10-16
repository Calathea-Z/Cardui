using Cardui.Api.Domain.Recovery;

namespace Cardui.Api.Dtos.Plan;

/// <summary>
/// One debt in the payoff order captured for a decision plan.
/// A null minimum, rate, or payoff date was unknown. The balance is the amount the plan used.
/// </summary>
public class DecisionPlanDebtDto
{
    public Guid DebtId { get; set; }

    public required string Name { get; set; }

    public decimal Balance { get; set; }

    public decimal? Minimum { get; set; }

    public decimal? Apr { get; set; }

    public DebtScheduleStop Stop { get; set; }

    public DateOnly? PaidOffOn { get; set; }
}
