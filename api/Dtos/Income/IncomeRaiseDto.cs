namespace Cardui.Api.Dtos.Income;

public class IncomeRaiseDto
{
    public Guid Id { get; set; }

    public DateOnly EffectiveDate { get; set; }

    /// <summary>
    /// The typical net pay for one payment from EffectiveDate.
    /// It does not replace the source's current amount.
    /// </summary>
    public decimal TakeHomeAmount { get; set; }
}
