namespace Cardui.Api.Dtos.Plan;

/// <summary>
/// The cash preview selected for a decision plan.
/// A null spending or floor amount means the plan used the household's saved figure.
/// </summary>
public class DecisionPlanScenarioDto
{
    public decimal MonthlyExtra { get; set; }

    public int IncomeLossMonths { get; set; }

    public decimal WindfallAmount { get; set; }

    public DateOnly? WindfallOn { get; set; }

    public decimal? LivingSpendingMonthly { get; set; }

    public decimal? CashFloor { get; set; }
}
