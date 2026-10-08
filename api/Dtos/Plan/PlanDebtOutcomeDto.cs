using Cardui.Api.Domain.Recovery;

namespace Cardui.Api.Dtos.Plan;

public class PlanDebtOutcomeDto
{
    public Guid DebtId { get; set; }

    public required string Name { get; set; }

    /// <summary>
    /// Why the projection stopped for this debt. Anything but PaidOff blocks the plan.
    /// </summary>
    public DebtScheduleStop Stop { get; set; }

    /// <summary>
    /// The opening balance.
    /// </summary>
    public decimal Balance { get; set; }

    /// <summary>
    /// The monthly minimum. Null when a rate, minimum, or due date is missing.
    /// </summary>
    public decimal? Minimum { get; set; }

    public DateOnly? PaidOffOn { get; set; }

    /// <summary>
    /// Interest charged in the last modeled month. For a debt that does not pay down, that is the month its payment fell short.
    /// Null when no payment could be modeled.
    /// </summary>
    public decimal? LastMonthInterest { get; set; }

    /// <summary>
    /// The payment in that same month, including extra and rolled cash. Null when no payment could be modeled.
    /// </summary>
    public decimal? LastMonthPayment { get; set; }
}
