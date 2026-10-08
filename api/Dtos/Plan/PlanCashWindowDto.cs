namespace Cardui.Api.Dtos.Plan;

public class PlanCashWindowDto
{
    public DateOnly From { get; set; }

    public DateOnly Through { get; set; }

    public decimal EndingCash { get; set; }

    /// <summary>
    /// The smallest ending cash in the window, on the first day that reaches it.
    /// </summary>
    public decimal LowestCash { get; set; }

    public DateOnly LowestCashOn { get; set; }

    /// <summary>
    /// True when any day in the window ends below zero.
    /// </summary>
    public bool CashShortfall { get; set; }
}
