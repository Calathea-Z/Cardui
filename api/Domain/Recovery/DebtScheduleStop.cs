namespace Cardui.Api.Domain.Recovery;

/// <summary>
/// Why a debt projection stopped.
/// PaidOff means the balance reached zero. DoesNotPayDown means a payment left the balance the same or higher.
/// RateUnknown and MinimumUnknown mean a later month would have to invent a term.
/// DueDateUnknown means there is no date to put a payment on. HorizonReached means the window or the month cap ended first.
/// </summary>
public enum DebtScheduleStop
{
    PaidOff,
    DoesNotPayDown,
    RateUnknown,
    MinimumUnknown,
    DueDateUnknown,
    HorizonReached
}
