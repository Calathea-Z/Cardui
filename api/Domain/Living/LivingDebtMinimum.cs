namespace Cardui.Api.Domain.Living;

/// <summary>
/// One debt's minimum payment for the monthly picture.
/// Balance is null when the debt is not ready for the plan. A known zero balance has no active payment.
/// Amount is null when the minimum is unknown. Zero is a known zero. The amount is one month.
/// </summary>
public sealed record LivingDebtMinimum(
    string Name,
    decimal? Balance,
    decimal? Amount,
    string Currency);
