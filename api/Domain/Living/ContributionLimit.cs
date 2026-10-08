namespace Cardui.Api.Domain.Living;

/// <summary>
/// How a person's monthly amount relates to their recorded pay.
/// The stored contribution and the API use the member name.
/// FullPay means the amount is blank, so every recorded paycheck stays shared.
/// Shared means the plan uses only the monthly amount. AllRecordedPay means the amount is at least their scheduled pay.
/// Unplaced means the amount is set and there is no scheduled paycheck to spread it across.
/// </summary>
public enum ContributionLimit
{
    FullPay,
    Shared,
    AllRecordedPay,
    Unplaced
}
