namespace Cardui.Api.Domain.Transactions;

/// <summary>
/// Merchant history grouped into periods, including the period that contains today.
/// </summary>
public sealed record MerchantHistorySeries(
    string Granularity,
    string SelectedPeriodKey,
    IReadOnlyList<MerchantHistoryPeriod> Periods);
