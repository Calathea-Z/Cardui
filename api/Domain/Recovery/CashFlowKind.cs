namespace Cardui.Api.Domain.Recovery;

/// <summary>
/// What a dated cash amount is.
/// Income is money in. A bill, a debt payment, living spending, and a savings contribution are money out.
/// Living spending leaves cash. A savings contribution reserves cash and leaves cash as it is.
/// </summary>
public enum CashFlowKind
{
    Income,
    Bill,
    DebtPayment,
    Savings,
    LivingSpending
}
