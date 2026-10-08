namespace Cardui.Api.Domain.Recovery;

/// <summary>
/// Cash across a stretch of days, including both ends.
/// Ending amounts are the last day. LowestCash is the smallest ending cash, on the first day that hits it.
/// A shortfall flag is true when any day in the stretch is short.
/// </summary>
public sealed record CashWindow(
    DateOnly From,
    DateOnly Through,
    decimal EndingCash,
    decimal EndingAvailable,
    decimal EndingReserve,
    decimal LowestCash,
    DateOnly LowestCashOn,
    bool CashShortfall,
    bool ReserveShortfall);
