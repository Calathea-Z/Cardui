using Cardui.Api.Models;

namespace Cardui.Api.Dtos.Plan;

/// <summary>
/// One saved plan version and the decision plan it captured.
/// Number starts at 1 for the household. AcceptedAt and SupersededAt are null until that change happens.
/// </summary>
public class PlanVersionDto
{
    public Guid Id { get; set; }

    public int Number { get; set; }

    public PlanVersionStatus Status { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? AcceptedAt { get; set; }

    public DateTimeOffset? SupersededAt { get; set; }

    public required DecisionPlanDto Plan { get; set; }
}
