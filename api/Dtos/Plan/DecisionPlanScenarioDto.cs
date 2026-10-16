using Cardui.Api.Domain.Recovery;

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

    /// <summary>
    /// Who extra and a rolled payment follow. An older saved plan is avalanche.
    /// </summary>
    public PayoffPriorityChoice Priority { get; set; }

    /// <summary>
    /// The constrained debt. Null unless <see cref="Priority"/> is the user constraint.
    /// </summary>
    public Guid? ConstraintDebtId { get; set; }

    /// <summary>
    /// Pay first or pay last. Null unless <see cref="Priority"/> is the user constraint.
    /// </summary>
    public PayoffConstraintKind? ConstraintKind { get; set; }

    /// <summary>
    /// How much freed cash this plan keeps each month. Zero unless the plan keeps some.
    /// </summary>
    public decimal ReclaimAmount { get; set; }
}
