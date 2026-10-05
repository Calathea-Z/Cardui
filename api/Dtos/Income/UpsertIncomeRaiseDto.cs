namespace Cardui.Api.Dtos.Income;

public class UpsertIncomeRaiseDto
{
    public DateOnly EffectiveDate { get; set; }

    public decimal TakeHomeAmount { get; set; }
}
