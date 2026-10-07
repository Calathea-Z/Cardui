using Cardui.Api.Domain.Accounts;
using Xunit;

namespace Cardui.Tests.Domain;

public class AccountLedgerTests
{
    private static readonly DateOnly Opening = new(2026, 10, 1);

    [Fact]
    public void BalanceAsOf_DecreasesCashWhenMoneyLeaves()
    {
        var balance = AccountLedger.BalanceAsOf(
            AccountTypes.Depository,
            openingBalance: 1_000m,
            Opening,
            [
                new LedgerTransaction(new DateOnly(2026, 10, 2), 40m, Pending: false, Archived: false),
                new LedgerTransaction(new DateOnly(2026, 10, 2), -15m, Pending: false, Archived: false),
                new LedgerTransaction(new DateOnly(2026, 10, 3), 10m, Pending: true, Archived: false),
                new LedgerTransaction(new DateOnly(2026, 10, 3), 25m, Pending: false, Archived: true),
                new LedgerTransaction(new DateOnly(2026, 9, 30), 100m, Pending: false, Archived: false)
            ],
            asOf: new DateOnly(2026, 10, 3));

        Assert.Equal(975m, balance);
    }

    [Fact]
    public void BalanceAsOf_IncreasesAmountOwedWhenACreditPurchasePosts()
    {
        var balance = AccountLedger.BalanceAsOf(
            AccountTypes.Credit,
            openingBalance: 200m,
            Opening,
            [
                new LedgerTransaction(new DateOnly(2026, 10, 2), 40m, Pending: false, Archived: false),
                new LedgerTransaction(new DateOnly(2026, 10, 2), -25m, Pending: false, Archived: false)
            ],
            asOf: new DateOnly(2026, 10, 2));

        Assert.Equal(215m, balance);
    }

    [Fact]
    public void TransactionAmountForBalanceChange_UsesTheAccountSign()
    {
        Assert.Equal(-30m, AccountLedger.TransactionAmountForBalanceChange(
            AccountTypes.Depository,
            balanceChange: 30m));
        Assert.Equal(-30m, AccountLedger.TransactionAmountForBalanceChange(
            AccountTypes.Credit,
            balanceChange: -30m));
    }
}
