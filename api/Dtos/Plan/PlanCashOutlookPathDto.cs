namespace Cardui.Api.Dtos.Plan;

public class PlanCashOutlookPathDto
{
    /// <summary>
    /// Cash at each source's typical pay, with debt payments from this path.
    /// </summary>
    public required PlanCashForecastDto Typical { get; set; }

    /// <summary>
    /// Cash at low pay where recorded, without expected raises. Null when no source records a low amount.
    /// </summary>
    public PlanCashForecastDto? LowPay { get; set; }
}
