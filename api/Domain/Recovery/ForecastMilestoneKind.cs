namespace Cardui.Api.Domain.Recovery;

/// <summary>
/// A dated point in a forecast.
/// A shortfall is the first day that amount is below zero.
/// Recovered and Restored are the first later day it is back to zero or above.
/// DebtPaidOff is the due date that clears a balance.
/// </summary>
public enum ForecastMilestoneKind
{
    CashShortfall,
    ReserveShortfall,
    DebtPaidOff,
    CashRecovered,
    ReserveRestored
}
