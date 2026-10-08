namespace Cardui.Api.Dtos.Plan;

public class PlanRecoveryStepDto
{
    public Guid DebtId { get; set; }

    public required string Name { get; set; }

    /// <summary>
    /// The due date that brought the balance to zero. The minimum is still paid that month.
    /// </summary>
    public DateOnly EndedOn { get; set; }

    /// <summary>
    /// The date the minimum is no longer paid.
    /// Null when that date falls outside the projection.
    /// </summary>
    public DateOnly? StartsOn { get; set; }

    public decimal Minimum { get; set; }

    /// <summary>
    /// Recurring freed cash after this step. It does not include shared extra.
    /// </summary>
    public decimal BreathingRoom { get; set; }
}
