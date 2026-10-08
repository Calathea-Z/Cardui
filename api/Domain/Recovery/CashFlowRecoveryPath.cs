namespace Cardui.Api.Domain.Recovery;

/// <summary>
/// When each payoff on one rollover path removes a monthly obligation, and the breathing room that follows.
/// StartingObligation is the known minimums before any payoff. It is null when every minimum is unknown.
/// RemainingObligation is the known minimums of debts that are not paid off. It is zero when none remain,
/// and null when every remaining debt is missing a rate, minimum, or due date.
/// UnknownRemaining counts those remaining debts. BreathingRoom is the freed cash still uncommitted after the last step.
/// ReleasedExtra is shared extra that is no longer sent once every debt is paid off, and zero until then.
/// RecurringRoom is BreathingRoom plus ReleasedExtra, the monthly amount available after the last recorded change.
/// </summary>
public sealed record CashFlowRecoveryPath(
    PayoffRolloverKind Kind,
    IReadOnlyList<CashFlowRecoveryStep> Steps,
    decimal? StartingObligation,
    decimal? RemainingObligation,
    int UnknownRemaining,
    decimal BreathingRoom,
    decimal ReleasedExtra,
    decimal RecurringRoom,
    string Explanation);
