namespace Cardui.Api.Domain;

/// <summary>
/// Planning-currency account totals, plus the accounts left out because
/// their currency does not match.
/// </summary>
internal sealed record AccountTotalResult(
    AccountTotals Totals,
    int ExcludedAccountCount,
    IReadOnlyList<string> ExcludedCurrencies);
