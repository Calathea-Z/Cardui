namespace Cardui.Api.Domain.Recovery;

/// <summary>
/// The cash forecast for one payoff path.
/// Typical uses each source's typical pay. LowPay uses low pay where recorded, and it is null when no source has a low amount.
/// </summary>
public sealed record HouseholdCashOutlookPath(
    PayoffRolloverKind Kind,
    CashForecastReport Typical,
    CashForecastReport? LowPay);
