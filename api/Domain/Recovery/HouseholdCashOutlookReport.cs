namespace Cardui.Api.Domain.Recovery;

/// <summary>
/// The Plan cash outlook on rollover and on keeping every freed payment.
/// StartingCash is the cash the forecasts start from on AsOf.
/// HasIncome and HasBills are true when at least one source or bill counts in the planning currency.
/// ExcludedCurrencies are income, bill, and debt codes the forecasts left out.
/// </summary>
public sealed record HouseholdCashOutlookReport(
    DateOnly AsOf,
    decimal StartingCash,
    bool HasIncome,
    bool HasBills,
    IReadOnlyList<string> ExcludedCurrencies,
    HouseholdCashOutlookPath Rollover,
    HouseholdCashOutlookPath ReclaimAll);
