namespace Cardui.Api.Dtos.Plan;

public class PlanCashForecastDto
{
    /// <summary>
    /// Ending cash for each of the first 30 days, including days with nothing due.
    /// </summary>
    public IReadOnlyList<PlanCashDayDto> Days { get; set; } = [];

    public required PlanCashWindowDto DayView { get; set; }

    /// <summary>
    /// The 6, 12, and 18 month horizons, in that order.
    /// </summary>
    public IReadOnlyList<PlanCashHorizonDto> Horizons { get; set; } = [];

    /// <summary>
    /// The first day cash goes below zero inside 18 months. Null when it never does.
    /// </summary>
    public DateOnly? ShortfallOn { get; set; }

    /// <summary>
    /// The first day after ShortfallOn that cash is back to zero or above. Null when it does not recover inside 18 months.
    /// </summary>
    public DateOnly? RecoveredOn { get; set; }

    /// <summary>
    /// The first day what is left after the reserve goes below zero. Null when it never does.
    /// </summary>
    public DateOnly? ReserveShortfallOn { get; set; }

    /// <summary>
    /// The first day after ReserveShortfallOn that the leftover is back to zero or above. Null when it does not recover.
    /// </summary>
    public DateOnly? ReserveRestoredOn { get; set; }
}
