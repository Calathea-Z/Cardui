namespace Cardui.Api.Domain.Accounts;

/// <summary>
/// The cash total used to start Plan, with enough provenance to explain that claim.
/// OldestBalanceAsOf is the oldest latest snapshot among included cash accounts.
/// A connected balance is stale when its latest snapshot is missing or more than two days old.
/// </summary>
public sealed record CashPosition(
    decimal Total,
    int AccountCount,
    int ManualAccountCount,
    int ConnectedAccountCount,
    DateOnly? OldestBalanceAsOf,
    int UnknownBalanceDateCount,
    int StaleConnectedAccountCount);
