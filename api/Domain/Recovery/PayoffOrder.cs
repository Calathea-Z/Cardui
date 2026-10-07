namespace Cardui.Api.Domain.Recovery;

/// <summary>
/// One ranking and what it costs.
/// DebtIds is the order shared extra follows, with debts that cannot be calculated at the end.
/// TotalInterest is the sum of each debt's interest. PaidOffOn is the last due date that clears a calculable debt,
/// and it is null when any calculable debt is still open or a term is missing.
/// Debts follows DebtIds. A freed minimum is not rolled into the next debt.
/// </summary>
public sealed record PayoffOrder(
    PayoffOrderKind Kind,
    IReadOnlyList<Guid> DebtIds,
    decimal TotalInterest,
    DateOnly? PaidOffOn,
    IReadOnlyList<PayoffDebtOutcome> Debts);
