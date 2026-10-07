namespace Cardui.Api.Domain.Debts;

/// <summary>
/// One account that has at least one match signal, before a rank is assigned.
/// </summary>
internal sealed record DebtRankedMatch(
    DebtMatchCandidate Account,
    IReadOnlyList<DebtMatchReason> Reasons);
