using Cardui.Api.Domain.Accounts;

namespace Cardui.Api.Domain.Savings;

public static class SavingsAccounts
{
    /// <summary>
    /// True when a goal may follow this account.
    /// It has to be an active cash account that is not archived, in the planning currency. A blank currency counts.
    /// </summary>
    public static bool CanFollow(
        string type,
        string? currency,
        bool isActive,
        DateTimeOffset? archivedAt,
        string planningCurrency)
    {
        return isActive
            && archivedAt is null
            && AccountTypes.IsCash(type)
            && PlanningCurrencyRules.IsIncluded(currency, planningCurrency);
    }
}
