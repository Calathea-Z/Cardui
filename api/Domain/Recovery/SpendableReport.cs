namespace Cardui.Api.Domain.Recovery;

/// <summary>
/// The spendable estimate on both payoff paths.
/// Each path uses that path's debt payments. The extra, when one is tried, is compared with the same path at minimums.
/// </summary>
public sealed record SpendableReport(
    SpendableEstimate Rollover,
    SpendableEstimate ReclaimAll);
