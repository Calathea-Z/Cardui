namespace Cardui.Api.Domain;

public static class AccountBalanceHistory
{
    /// <summary>
    /// Builds one history point per snapshot date. An account with no new
    /// snapshot keeps the balance from its previous point.
    /// </summary>
    public static IReadOnlyList<AccountBalanceHistoryPoint> Build(
        IEnumerable<AccountSnapshotBalance> snapshots)
    {
        var carriedBalances = new Dictionary<Guid, AccountBalanceValue>();
        var points = new List<AccountBalanceHistoryPoint>();

        foreach (var day in snapshots
            .GroupBy(x => x.Date)
            .OrderBy(x => x.Key))
        {
            foreach (var snapshot in day.OrderBy(x => x.RecordedAt))
            {
                carriedBalances[snapshot.AccountId] = new AccountBalanceValue(
                    snapshot.Type,
                    snapshot.CurrentBalance);
            }

            var totals = AccountTotalsCalculator.Calculate(carriedBalances.Values);
            points.Add(new AccountBalanceHistoryPoint(
                day.Key,
                totals.Cash,
                totals.Investments,
                totals.CreditCards,
                totals.Loans,
                totals.NetWorth));
        }

        return points;
    }
}
