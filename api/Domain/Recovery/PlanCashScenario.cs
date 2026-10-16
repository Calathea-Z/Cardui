namespace Cardui.Api.Domain.Recovery;

/// <summary>
/// A temporary cash preview. None of these amounts is saved.
/// MonthlyExtra is shared extra for the payoff path. Zero is minimums only.
/// IncomeLossMonths leaves out included pay from the forecast start, then pay resumes. Zero keeps every paycheck.
/// WindfallAmount arrives once on WindfallOn and stays in cash. It does not pay a debt or raise the reserve.
/// LivingSpendingMonthly replaces later full months of flexible spending. Null keeps the saved amount.
/// CashFloor replaces the saved cash-to-keep portion of the reserve. Null keeps the saved floor. Other set-aside amounts stay.
/// </summary>
public sealed record PlanCashScenario(
    decimal MonthlyExtra,
    int IncomeLossMonths,
    decimal WindfallAmount,
    DateOnly? WindfallOn,
    decimal? LivingSpendingMonthly,
    decimal? CashFloor)
{
    /// <summary>
    /// The longest income-loss preview, matching the 18-month forecast.
    /// </summary>
    public const int MaxMonths = 18;

    /// <summary>
    /// The saved plan: minimums only, every paycheck, no windfall, and the saved spending and floor.
    /// </summary>
    public static PlanCashScenario None { get; } = new(0m, 0, 0m, null, null, null);

    /// <summary>
    /// Stable id for the one-time windfall event. It is not an income source.
    /// </summary>
    public static readonly Guid WindfallSourceId = new("c4a0d000-0000-4000-8000-000000000001");
}
