namespace Cardui.Api.Dtos.Plan;

/// <summary>
/// Temporary amounts tried on GET /api/plan/recovery. Nothing is saved.
/// Omit a nullable amount to keep the saved Plan budget or cash floor.
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
}
