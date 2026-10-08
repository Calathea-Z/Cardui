namespace Cardui.Api.Dtos.Plan;

public class PlanBalancePointDto
{
    public Guid DebtId { get; set; }

    public DateOnly DueDate { get; set; }

    /// <summary>
    /// The balance right after this payment. Zero once the debt is paid off.
    /// </summary>
    public decimal Balance { get; set; }
}
