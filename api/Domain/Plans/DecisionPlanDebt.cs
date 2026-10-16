using Cardui.Api.Domain.Recovery;

namespace Cardui.Api.Domain.Plans;

/// <summary>
/// One debt in the payoff order captured for this plan.
/// Balance, minimum, and rate are the values used when the version was saved. A later edit to the debt does not change them.
/// Stop is why the projection ended. PaidOff means the balance reached zero.
/// </summary>
public sealed record DecisionPlanDebt(
    Guid DebtId,
    string Name,
    decimal Balance,
    decimal? Minimum,
    decimal? Apr,
    DebtScheduleStop Stop,
    DateOnly? PaidOffOn);
