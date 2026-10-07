using Cardui.Api.Domain.Accounts;
using Cardui.Api.Models;

namespace Cardui.Api.Domain;

/// <summary>
/// Decides whether a debt may follow an account.
/// A manual account is excluded. Its balance is already one the person types.
/// </summary>
public static class DebtFollowEligibility
{
    /// <summary>
    /// True when the account is an active connected credit card or loan in the debt's currency.
    /// An archived account, a cash account, or a different currency cannot be followed.
    /// </summary>
    public static bool CanFollow(
        FinancialRecordSource source,
        bool hasPlaidItem,
        bool isActive,
        bool isArchived,
        string accountType,
        string? accountCurrency,
        string debtCurrency)
    {
        if (source != FinancialRecordSource.Plaid || !hasPlaidItem || !isActive || isArchived)
        {
            return false;
        }

        if (!AccountLedger.IsLiability(accountType))
        {
            return false;
        }

        var accountCode = Normalize(accountCurrency);
        var debtCode = Normalize(debtCurrency);
        return accountCode is not null
            && debtCode is not null
            && string.Equals(accountCode, debtCode, StringComparison.OrdinalIgnoreCase);
    }

    #region Private Methods

    /// <summary>
    /// Trims a currency code. Blank stays null so it cannot match by accident.
    /// </summary>
    private static string? Normalize(string? currency)
    {
        var trimmed = currency?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    #endregion
}
