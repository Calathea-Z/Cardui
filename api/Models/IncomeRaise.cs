namespace Cardui.Api.Models;

public class IncomeRaise
{
    public Guid Id { get; init; }

    public Guid IncomeSourceId { get; set; }

    public IncomeSource? IncomeSource { get; set; }

    public DateOnly EffectiveDate { get; set; }

    public decimal TakeHomeAmount { get; set; }
}
