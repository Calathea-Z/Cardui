namespace Cardui.Api.Domain.Debts;

/// <summary>
/// One plain reason an account was suggested.
/// Mask is the digits found in the debt name. Words are the shared name words, in the debt's spelling.
/// Difference is the absolute gap between the two balances. Zero means they are the same amount.
/// </summary>
public sealed record DebtMatchReason(
    DebtMatchReasonKind Kind,
    string? Mask,
    IReadOnlyList<string> Words,
    decimal? Difference);
