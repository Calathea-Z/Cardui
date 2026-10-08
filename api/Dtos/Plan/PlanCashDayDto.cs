namespace Cardui.Api.Dtos.Plan;

public class PlanCashDayDto
{
    public DateOnly Date { get; set; }

    /// <summary>
    /// Cash at the end of the day. Negative is a shortfall.
    /// </summary>
    public decimal Cash { get; set; }

    /// <summary>
    /// Income that arrived this day. Zero or more.
    /// </summary>
    public decimal Income { get; set; }

    /// <summary>
    /// Bills paid this day. Zero or more.
    /// </summary>
    public decimal Bills { get; set; }

    /// <summary>
    /// Debt payments made this day. Zero or more.
    /// </summary>
    public decimal DebtPayments { get; set; }

    /// <summary>
    /// Everyday spending counted this day. Zero or more. It leaves cash and is not a bill.
    /// </summary>
    public decimal EverydaySpending { get; set; }
}
