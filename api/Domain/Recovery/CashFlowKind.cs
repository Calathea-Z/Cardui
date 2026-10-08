namespace Cardui.Api.Domain.Recovery;

/// <summary>
/// What a dated cash amount is.
/// Income is money in. A bill, a debt payment, and a savings contribution are money out.
/// A savings contribution reserves cash. It is not a bill and it is not spending.
/// </summary>
public enum CashFlowKind
{
    Income,
    Bill,
    DebtPayment,
    Savings
}
