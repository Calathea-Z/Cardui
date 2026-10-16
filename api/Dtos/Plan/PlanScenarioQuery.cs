using Cardui.Api.Domain.Recovery;

namespace Cardui.Api.Dtos.Plan;

/// <summary>
/// Temporary amounts tried on GET /api/plan/recovery. Nothing is saved.
/// Omit a nullable amount to keep the saved Plan budget or cash floor.
/// Priority chooses who extra and a rolled payment follow. ReclaimAmount is how much freed cash to keep each month.
/// </summary>
public class PlanScenarioQuery
{
    /// <summary>
    /// Shared extra aimed at the first debt that can take it. Zero is minimums only.
    /// </summary>
    public decimal MonthlyExtra { get; set; }

    /// <summary>
    /// Whole months from today with no included pay. Zero keeps every paycheck.
    /// </summary>
    public int IncomeLossMonths { get; set; }

    /// <summary>
    /// One-time cash added on <see cref="WindfallOn"/>. Zero means no windfall.
    /// </summary>
    public decimal WindfallAmount { get; set; }

    /// <summary>
    /// The date a positive windfall arrives. Null when there is no windfall.
    /// </summary>
    public DateOnly? WindfallOn { get; set; }

    /// <summary>
    /// Flexible spending tried instead of the saved monthly amount. Null keeps the saved amount. Zero removes later full months.
    /// </summary>
    public decimal? LivingSpendingMonthly { get; set; }

    /// <summary>
    /// Cash to keep tried instead of the saved floor. Null keeps the saved floor. Other set-aside amounts stay.
    /// </summary>
    public decimal? CashFloor { get; set; }

    /// <summary>
    /// Who extra and a rolled payment follow. Avalanche is the baseline when this is omitted.
    /// </summary>
    public PayoffPriorityChoice Priority { get; set; }

    /// <summary>
    /// The debt to pay first or last. Null when this preview has no constraint.
    /// </summary>
    public Guid? ConstraintDebtId { get; set; }

    /// <summary>
    /// Pay first or pay last for <see cref="ConstraintDebtId"/>. Null when this preview has no constraint.
    /// </summary>
    public PayoffConstraintKind? ConstraintKind { get; set; }

    /// <summary>
    /// How much freed cash to keep each month on the partial path. Zero keeps none.
    /// </summary>
    public decimal ReclaimAmount { get; set; }
}
