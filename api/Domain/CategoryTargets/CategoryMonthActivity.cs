namespace Cardui.Api.Domain.CategoryTargets;

/// <summary>
/// Posted spending for one month, after income, transfers, and other currencies are left out.
/// </summary>
internal sealed record CategoryMonthActivity(
    IReadOnlyList<CategoryMonthSpent> Spent,
    int ExcludedTransactionCount,
    IReadOnlyList<string> ExcludedCurrencies,
    IReadOnlyDictionary<Guid, ActivityCategoryLabel> Labels);
