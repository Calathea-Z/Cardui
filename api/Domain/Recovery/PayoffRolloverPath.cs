namespace Cardui.Api.Domain.Recovery;

/// <summary>
/// One way of treating cash after a debt's payment ends.
/// DebtIds is the order extra follows. TotalInterest is the sum of each debt's interest.
/// PaidOffOn is the last due date that clears a calculable debt, and it is null when any calculable debt is still open.
/// FreedPayments lists each debt whose balance reached zero, including the last one, which has no next debt to receive it.
/// StartsOn on each payment is the next due date, when that minimum is no longer paid.
/// BalancePoints holds each debt's balance after every modeled payment, by due date and then by DebtIds order.
/// A debt that could not be calculated has no points.
/// CashReclaimed is freed cash kept for savings or spending while a debt was still open. CashRolled is freed cash applied to a later debt.
/// InterestDifference is this path's interest minus rollover. It is zero on the rollover path.
/// </summary>
public sealed record PayoffRolloverPath(
    PayoffRolloverKind Kind,
    IReadOnlyList<Guid> DebtIds,
    decimal TotalInterest,
    DateOnly? PaidOffOn,
    IReadOnlyList<PayoffDebtOutcome> Debts,
    IReadOnlyList<PayoffFreedPayment> FreedPayments,
    IReadOnlyList<PayoffBalancePoint> BalancePoints,
    decimal CashReclaimed,
    decimal CashRolled,
    decimal InterestDifference,
    string Explanation);
