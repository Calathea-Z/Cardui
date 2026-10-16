namespace Cardui.Api.Dtos.Plan;

/// <summary>
/// Cash due now compared with cash on hand, the reserve, and cash to keep.
/// This is not an investment return. LeavesLessThanCashToKeep is false when no floor is saved.
/// </summary>
public sealed class RestructuringCashEffectDto
{
    public decimal CashOnHand { get; set; }

    public decimal Reserve { get; set; }

    public decimal CashToKeep { get; set; }

    public bool HasCashFloor { get; set; }

    public bool ExceedsCashOnHand { get; set; }

    public bool UsesReserve { get; set; }

    public bool LeavesLessThanCashToKeep { get; set; }
}
