namespace Cardui.Api.Domain.Recovery;

/// <summary>
/// The forecast at 6, 12, or 18 months.
/// Window is cash through that month. Debts are the balances still owed then.
/// MinimumObligation is the monthly minimum still due on debts with a known payment.
/// It is null when every remaining debt is missing a rate, minimum, or due date.
/// UnknownMinimumCount is how many remaining debts were left out of that sum.
/// </summary>
public sealed record CashHorizon(
    int Months,
    CashWindow Window,
    decimal? MinimumObligation,
    int UnknownMinimumCount,
    IReadOnlyList<ForecastDebtBalance> Debts);
