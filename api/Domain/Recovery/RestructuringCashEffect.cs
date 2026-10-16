namespace Cardui.Api.Domain.Recovery;

/// <summary>
/// What upfront cash does to cash on hand, the reserve, and cash to keep.
/// ExceedsCashOnHand means the cash due is more than the cash accounts show.
/// UsesReserve means it spends cash already set aside. LeavesLessThanCashToKeep is false when no floor is saved.
/// This is not an investment return.
/// </summary>
public sealed record RestructuringCashEffect(
    decimal CashOnHand,
    decimal Reserve,
    decimal CashToKeep,
    bool HasCashFloor,
    bool ExceedsCashOnHand,
    bool UsesReserve,
    bool LeavesLessThanCashToKeep);
