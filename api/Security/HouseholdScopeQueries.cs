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
            ? query.Where(x =>
                (x.PlaidItemId != null && x.PlaidItem!.HouseholdId == householdId)
                || (x.PlaidItemId == null && x.HouseholdId == householdId))
            : query.Where(x =>
                (x.PlaidItemId != null && x.PlaidItem!.HouseholdId == null)
                || (x.PlaidItemId == null && x.HouseholdId == null));
    }

    public static IQueryable<Transaction> InHousehold(
        this IQueryable<Transaction> query,
        HouseholdScope scope)
    {
        scope.EnsureBound();

        return scope.HouseholdId is Guid householdId
            ? query.Where(x =>
                (x.Account.PlaidItemId != null && x.Account.PlaidItem!.HouseholdId == householdId)
                || (x.Account.PlaidItemId == null && x.Account.HouseholdId == householdId))
            : query.Where(x =>
                (x.Account.PlaidItemId != null && x.Account.PlaidItem!.HouseholdId == null)
                || (x.Account.PlaidItemId == null && x.Account.HouseholdId == null));
    }

    public static IQueryable<AccountBalanceSnapshot> InHousehold(
        this IQueryable<AccountBalanceSnapshot> query,
        HouseholdScope scope)
    {
        scope.EnsureBound();

        return scope.HouseholdId is Guid householdId
            ? query.Where(x =>
                (x.Account.PlaidItemId != null && x.Account.PlaidItem!.HouseholdId == householdId)
                || (x.Account.PlaidItemId == null && x.Account.HouseholdId == householdId))
            : query.Where(x =>
                (x.Account.PlaidItemId != null && x.Account.PlaidItem!.HouseholdId == null)
                || (x.Account.PlaidItemId == null && x.Account.HouseholdId == null));
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
