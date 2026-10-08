namespace Cardui.Api.Domain.Recovery;

/// <summary>
/// What a dated cash amount is.
/// Income is money in. A bill, a debt payment, everyday spending, and a savings contribution are money out.
/// Everyday spending leaves cash. A savings contribution reserves cash and leaves cash as it is.
/// </summary>
public enum CashFlowKind
{
    Income,
    Bill,
    DebtPayment,
    Savings,
    EverydaySpending
}
