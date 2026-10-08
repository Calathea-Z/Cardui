namespace Cardui.Api.Domain.Recovery;

/// <summary>
/// The rate and minimum for one debt's next payment.
/// Skip is set when the payment cannot be calculated. The other fields are then unused.
/// </summary>
public sealed record ResolvedDebtPayment(
    DebtScheduleStop? Skip,
    decimal Balance,
    DateOnly DueDate,
    decimal RatePercent,
    bool RateIsPromotional,
    decimal Minimum)
{
    /// <summary>
    /// True when a payment can be calculated.
    /// Skip is then null.
    /// </summary>
    public bool IsResolved => Skip is null;
}
