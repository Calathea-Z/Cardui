namespace Cardui.Api.Domain.Recovery;

/// <summary>
/// What it takes to reach a savings target, and what a chosen monthly amount does.
/// Remaining is the gap still to reserve. Amounts are cents.
/// ToHitDate spreads Remaining across the months from the start through the target date, with the last month trued up.
/// FromContribution is the chosen monthly amount until Remaining is met. It can finish on a different date.
/// DatePassed means the target date is already before the start, so the gap is due at the start.
/// TargetDateBeyondHorizon and ContributionDoesNotReach mean the 600 month cap ended before that path finished.
/// </summary>
public sealed record SavingsTargetPlan(
    decimal Remaining,
    bool AlreadyMet,
    bool DatePassed,
    bool TargetDateBeyondHorizon,
    bool ContributionDoesNotReach,
    decimal? AmountNeededPerMonth,
    decimal? FinalAmountNeeded,
    DateOnly? ContributionReachesOn,
    IReadOnlyList<SavingsContribution> ToHitDate,
    IReadOnlyList<SavingsContribution> FromContribution);
