using Cardui.Api.Models;

namespace Cardui.Api.Security;

public static class HouseholdScopeQueries
{
    public static IQueryable<PlaidItem> InHousehold(
        this IQueryable<PlaidItem> query,
        HouseholdScope scope)
    {
        scope.EnsureBound();

        return scope.HouseholdId is Guid householdId
            ? query.Where(x => x.HouseholdId == householdId)
            : query.Where(x => x.HouseholdId == null);
    }

    public static IQueryable<Account> InHousehold(
        this IQueryable<Account> query,
        HouseholdScope scope)
    {
        scope.EnsureBound();

        return scope.HouseholdId is Guid householdId
            ? query.Where(x => x.PlaidItem.HouseholdId == householdId)
            : query.Where(x => x.PlaidItem.HouseholdId == null);
    }

    public static IQueryable<Transaction> InHousehold(
        this IQueryable<Transaction> query,
        HouseholdScope scope)
    {
        scope.EnsureBound();

        return scope.HouseholdId is Guid householdId
            ? query.Where(x => x.Account.PlaidItem.HouseholdId == householdId)
            : query.Where(x => x.Account.PlaidItem.HouseholdId == null);
    }

    public static IQueryable<AccountBalanceSnapshot> InHousehold(
        this IQueryable<AccountBalanceSnapshot> query,
        HouseholdScope scope)
    {
        scope.EnsureBound();

        return scope.HouseholdId is Guid householdId
            ? query.Where(x => x.Account.PlaidItem.HouseholdId == householdId)
            : query.Where(x => x.Account.PlaidItem.HouseholdId == null);
    }

    public static IQueryable<Category> VisibleToHousehold(
        this IQueryable<Category> query,
        HouseholdScope scope)
    {
        scope.EnsureBound();

        return scope.HouseholdId is Guid householdId
            ? query.Where(x => x.IsSystem || x.HouseholdId == householdId)
            : query.Where(x => x.IsSystem || x.HouseholdId == null);
    }

    public static IQueryable<SubGroup> VisibleToHousehold(
        this IQueryable<SubGroup> query,
        HouseholdScope scope)
    {
        scope.EnsureBound();

        return scope.HouseholdId is Guid householdId
            ? query.Where(x => x.IsSystem || x.HouseholdId == householdId)
            : query.Where(x => x.IsSystem || x.HouseholdId == null);
    }
}
