namespace Cardui.Api.Domain.Debts;

/// <summary>
/// One eligible account the matcher can compare with a debt.
/// DatedBalance is the latest snapshot that has a date and the debt's currency.
/// It is null when that figure is missing, so a balance with no date is not a signal.
/// </summary>
public sealed record DebtMatchCandidate(
    Guid AccountId,
    string Name,
    string? OfficialName,
    string? InstitutionName,
    string? Mask,
    string AccountType,
    decimal? DatedBalance);
