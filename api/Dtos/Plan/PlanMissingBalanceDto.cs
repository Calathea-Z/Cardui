namespace Cardui.Api.Dtos.Plan;

public class PlanMissingBalanceDto
{
    public Guid DebtId { get; set; }

    public required string Name { get; set; }
}
