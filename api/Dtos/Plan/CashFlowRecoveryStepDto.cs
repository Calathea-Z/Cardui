namespace Cardui.Api.Dtos.Plan;

public class CashFlowRecoveryStepDto
{
    public Guid DebtId { get; set; }

    public required string Name { get; set; }

    public DateOnly EndedOn { get; set; }

    /// <summary>
    /// The date the minimum is no longer paid.
    /// Null when that date falls outside the projection.
    /// </summary>
    public DateOnly? StartsOn { get; set; }

    public decimal Minimum { get; set; }

    public decimal Extra { get; set; }

    public decimal Amount { get; set; }

    public decimal BreathingRoomAdded { get; set; }

    /// <summary>
    /// Recurring freed cash after this step. It does not include shared extra.
    /// </summary>
    public decimal BreathingRoom { get; set; }
}
