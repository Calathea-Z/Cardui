namespace Cardui.Api.Domain.Plans;

/// <summary>
/// A dated result the recovery calculation actually produced.
/// The member name is the stored text. A milestone is omitted when that date or amount was not calculated.
/// </summary>
public enum DecisionPlanMilestoneKind
{
    CashShortfall,
    CashRecovered,
    ReserveShortfall,
    FirstPayoff,
    DebtFree,
    BreathingRoom
}
