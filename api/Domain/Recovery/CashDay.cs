namespace Cardui.Api.Domain.Recovery;

/// <summary>
/// Cash after one day's events.
/// Cash is the running balance. Reserve is the protected amount. Available is cash minus the reserve.
/// CashShortfall means cash is below zero. ReserveShortfall means available is below zero while a reserve is set aside.
/// Events are the amounts that landed that day, in income, bill, debt, then savings order.
/// </summary>
public sealed record CashDay(
    DateOnly Date,
    decimal Cash,
    decimal Available,
    decimal Reserve,
    bool CashShortfall,
    bool ReserveShortfall,
    IReadOnlyList<CashFlowEvent> Events);
