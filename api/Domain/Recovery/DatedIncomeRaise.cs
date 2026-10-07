namespace Cardui.Api.Domain.Recovery;

/// <summary>
/// A later take-home amount for one income payment.
/// Amount replaces the current payment on and after EffectiveDate. It is still one payment.
/// </summary>
public sealed record DatedIncomeRaise(
    DateOnly EffectiveDate,
    decimal Amount);
