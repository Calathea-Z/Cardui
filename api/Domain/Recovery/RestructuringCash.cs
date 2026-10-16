namespace Cardui.Api.Domain.Recovery;

/// <summary>
/// Cash the comparison reads when an offer uses money at the start.
/// CashOnHand is the cash-account total. Reserve is the amount already set aside.
/// CashToKeep is the floor inside that reserve. HasCashFloor is false when no floor is saved.
/// </summary>
public sealed record RestructuringCash(
    decimal CashOnHand,
    decimal Reserve,
    decimal CashToKeep,
    bool HasCashFloor);
