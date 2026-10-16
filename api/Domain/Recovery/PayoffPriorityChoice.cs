namespace Cardui.Api.Domain.Recovery;

/// <summary>
/// Which payment-priority alternative the plan follows.
/// Avalanche pays the highest rate first. MinimumRelease, Utilization, and Constraint are the other supported orders.
/// This is who receives extra and a rolled payment. It is not the date each debt is paid off.
/// The stored text is the member name.
/// </summary>
public enum PayoffPriorityChoice
{
    Avalanche,
    MinimumRelease,
    Utilization,
    Constraint
}
