namespace Cardui.Api.Domain.Debts;

/// <summary>
/// An eligible account suggested for a debt, with the reasons in display order.
/// Order is 1, 2, or 3. It is the rank, not a score, and it is not shown.
/// </summary>
public sealed record DebtAccountSuggestion(
    Guid AccountId,
    int Order,
    IReadOnlyList<DebtMatchReason> Reasons);
