namespace Cardui.Api.Domain.Recovery;

/// <summary>
/// One debt in a payoff comparison.
/// Terms are the amounts and dates already used to amortize a debt.
/// CreditLimit is the limit used for a revolving debt's utilization. A blank limit leaves utilization unknown.
/// An installment limit is ignored. Extra on the terms stays on that debt and is not part of the shared extra.
/// </summary>
public sealed record PayoffDebt(
    DebtAmortizationInput Terms,
    decimal? CreditLimit);
