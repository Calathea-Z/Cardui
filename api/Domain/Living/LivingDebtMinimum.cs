namespace Cardui.Api.Domain.Living;

/// <summary>
/// One debt's minimum payment for the monthly picture.
/// Amount is null when the minimum is unknown. Zero is a known zero. The amount is one month.
/// </summary>
public sealed record LivingDebtMinimum(
    string Name,
    decimal? Amount,
    string Currency);
