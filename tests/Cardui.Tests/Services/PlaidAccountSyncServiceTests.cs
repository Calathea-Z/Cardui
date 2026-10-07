using Cardui.Api.Data;
using Cardui.Api.Domain;
using Cardui.Api.Models;
using Cardui.Api.Security;
using Cardui.Api.Services.Plaid;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Xunit;
using PlaidAccount = Going.Plaid.Entity.Account;

namespace Cardui.Tests.Services;

public class PlaidAccountSyncServiceTests
{
    private static readonly DateTimeOffset SyncedAt =
        new(2026, 10, 7, 6, 30, 0, TimeSpan.Zero);

    [Fact]
    public async Task Sync_CopiesBalances_AndSkipsABlankAccountId()
    {
        await using var dbContext = CreateDbContext();
        var plaidItem = await SeedItemAsync(dbContext, "America/Los_Angeles");
        var existing = await SeedAccountAsync(dbContext, plaidItem, "acct-checking", 10m);
        var protector = new MappingAccessTokenProtector();
        var client = new ScriptedAccountsClient(
            CreatePlaidAccount("acct-checking", 42.50m, 40m),
            CreatePlaidAccount("acct-new", null, null, "Savings"),
            CreatePlaidAccount("  ", 9m, 9m));
        var service = CreateService(dbContext, client, protector);

        await service.SyncAccountsForPlaidItemAsync(plaidItem);

        Assert.Equal(["plain-token"], client.AccessTokens);

        var checking = await dbContext.Accounts.SingleAsync(account => account.Id == existing.Id);
        Assert.Equal("Prime Checking", checking.Name);
        Assert.Equal("Prime Checking Official", checking.OfficialName);
        Assert.Equal(AccountTypes.Depository, checking.Type);
        Assert.Equal("checking", checking.Subtype);
        Assert.Equal("1234", checking.Mask);
        Assert.Equal(42.50m, checking.CurrentBalance);
        Assert.Equal(40m, checking.AvailableBalance);
        Assert.Equal("USD", checking.IsoCurrencyCode);
        Assert.True(checking.IsActive);

        var created = await dbContext.Accounts.SingleAsync(account => account.PlaidAccountId == "acct-new");
        Assert.Equal(plaidItem.HouseholdId, created.HouseholdId);
        Assert.Equal(plaidItem.Id, created.PlaidItemId);
        Assert.Equal(FinancialRecordSource.Plaid, created.Source);
        Assert.Equal(0m, created.CurrentBalance);
        Assert.Null(created.AvailableBalance);
        Assert.Equal(2, await dbContext.Accounts.CountAsync());
    }

    [Fact]
    public async Task Sync_ReplacesTodaysSnapshot_InTheHouseholdTimeZone()
    {
        await using var dbContext = CreateDbContext();
        var plaidItem = await SeedItemAsync(dbContext, "America/Los_Angeles");
        var account = await SeedAccountAsync(dbContext, plaidItem, "acct-checking", 10m);
        var today = new DateOnly(2026, 10, 6);
        var yesterday = new DateOnly(2026, 10, 5);
        var todaySnapshot = await SeedSnapshotAsync(dbContext, account.Id, today, 10m);
        var yesterdaySnapshot = await SeedSnapshotAsync(dbContext, account.Id, yesterday, 5m);
        var service = CreateService(
            dbContext,
            new ScriptedAccountsClient(CreatePlaidAccount("acct-checking", 42.50m, 40m)));

        await service.SyncAccountsForPlaidItemAsync(plaidItem);

        var snapshots = await dbContext.AccountBalanceSnapshots
            .OrderBy(snapshot => snapshot.Date)
            .ToListAsync();
        Assert.Equal(2, snapshots.Count);
        Assert.Equal(yesterdaySnapshot.Id, snapshots[0].Id);
        Assert.Equal(5m, snapshots[0].CurrentBalance);
        Assert.Equal(today, snapshots[1].Date);
        Assert.NotEqual(todaySnapshot.Id, snapshots[1].Id);
        Assert.Equal(42.50m, snapshots[1].CurrentBalance);
        Assert.Equal(40m, snapshots[1].AvailableBalance);
        Assert.Equal("USD", snapshots[1].IsoCurrencyCode);
        Assert.DoesNotContain(snapshots, snapshot => snapshot.Date == new DateOnly(2026, 10, 7));
    }

    [Fact]
    public async Task Sync_WithoutAHousehold_UsesTheDefaultTimeZone()
    {
        await using var dbContext = CreateDbContext();
        var plaidItem = await SeedItemAsync(dbContext, timeZoneId: null);
        var account = await SeedAccountAsync(dbContext, plaidItem, "acct-checking", 10m);
        var service = CreateService(
            dbContext,
            new ScriptedAccountsClient(CreatePlaidAccount("acct-checking", 15m, 12m)),
            syncedAt: new DateTimeOffset(2026, 10, 7, 5, 30, 0, TimeSpan.Zero));

        await service.SyncAccountsForPlaidItemAsync(plaidItem);

        var snapshot = await dbContext.AccountBalanceSnapshots
            .SingleAsync(row => row.AccountId == account.Id);
        Assert.Equal(new DateOnly(2026, 10, 6), snapshot.Date);
        Assert.Equal(15m, snapshot.CurrentBalance);
    }

    [Fact]
    public async Task Sync_AccountMissingFromTheBank_IsMarkedInactive()
    {
        await using var dbContext = CreateDbContext();
        var plaidItem = await SeedItemAsync(dbContext, "America/Los_Angeles");
        var dropped = await SeedAccountAsync(dbContext, plaidItem, "acct-old", 8m);
        var alreadyInactive = await SeedAccountAsync(dbContext, plaidItem, "acct-gone", 3m);
        alreadyInactive.IsActive = false;
        alreadyInactive.UpdatedAt = SyncedAt.AddDays(-4);
        var kept = await SeedAccountAsync(dbContext, plaidItem, "acct-checking", 10m);
        await dbContext.SaveChangesAsync();
        var service = CreateService(
            dbContext,
            new ScriptedAccountsClient(CreatePlaidAccount("acct-checking", 11m, 11m)));

        await service.SyncAccountsForPlaidItemAsync(plaidItem);

        var storedDropped = await dbContext.Accounts.SingleAsync(account => account.Id == dropped.Id);
        var storedInactive = await dbContext.Accounts.SingleAsync(account => account.Id == alreadyInactive.Id);
        var storedKept = await dbContext.Accounts.SingleAsync(account => account.Id == kept.Id);
        Assert.False(storedDropped.IsActive);
        Assert.Equal(SyncedAt, storedDropped.UpdatedAt);
        Assert.False(storedInactive.IsActive);
        Assert.Equal(SyncedAt.AddDays(-4), storedInactive.UpdatedAt);
        Assert.True(storedKept.IsActive);
        Assert.Equal(11m, storedKept.CurrentBalance);
    }

    [Fact]
    public async Task Sync_LeavesAnArchivedAccountArchived()
    {
        await using var dbContext = CreateDbContext();
        var plaidItem = await SeedItemAsync(dbContext, "America/Los_Angeles");
        var archivedAt = SyncedAt.AddDays(-3);
        var returned = await SeedAccountAsync(dbContext, plaidItem, "acct-checking", 10m);
        returned.ArchivedAt = archivedAt;
        var dropped = await SeedAccountAsync(dbContext, plaidItem, "acct-old", 4m);
        dropped.ArchivedAt = archivedAt;
        await dbContext.SaveChangesAsync();
        var service = CreateService(
            dbContext,
            new ScriptedAccountsClient(CreatePlaidAccount("acct-checking", 12m, 12m)));

        await service.SyncAccountsForPlaidItemAsync(plaidItem);

        var storedReturned = await dbContext.Accounts.SingleAsync(account => account.Id == returned.Id);
        var storedDropped = await dbContext.Accounts.SingleAsync(account => account.Id == dropped.Id);
        Assert.Equal(archivedAt, storedReturned.ArchivedAt);
        Assert.Equal(12m, storedReturned.CurrentBalance);
        Assert.Equal(archivedAt, storedDropped.ArchivedAt);
        Assert.False(storedDropped.IsActive);
    }

    private static PlaidAccountSyncService CreateService(
        CarduiDBContext dbContext,
        ScriptedAccountsClient client,
        MappingAccessTokenProtector? protector = null,
        DateTimeOffset? syncedAt = null)
    {
        return new PlaidAccountSyncService(
            dbContext,
            client,
            protector ?? new MappingAccessTokenProtector(),
            NullLogger<PlaidAccountSyncService>.Instance,
            new FakeTimeProvider(syncedAt ?? SyncedAt));
    }

    private static CarduiDBContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CarduiDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new CarduiDBContext(options);
    }

    private static async Task<PlaidItem> SeedItemAsync(
        CarduiDBContext dbContext,
        string? timeZoneId)
    {
        var householdId = Guid.NewGuid();
        if (timeZoneId is not null)
        {
            dbContext.Households.Add(new Household
            {
                Id = householdId,
                OwnerClerkUserId = "owner",
                DisplayName = "Household",
                TimeZoneId = timeZoneId,
                CreatedAt = SyncedAt.AddDays(-30),
                UpdatedAt = SyncedAt.AddDays(-30)
            });
        }

        var plaidItem = new PlaidItem
        {
            Id = Guid.NewGuid(),
            HouseholdId = timeZoneId is null ? null : householdId,
            PlaidItemId = "item-1",
            AccessToken = "stored-token",
            CreatedAt = SyncedAt.AddDays(-10),
            UpdatedAt = SyncedAt.AddDays(-10)
        };
        dbContext.PlaidItems.Add(plaidItem);
        await dbContext.SaveChangesAsync();
        return plaidItem;
    }

    private static async Task<Account> SeedAccountAsync(
        CarduiDBContext dbContext,
        PlaidItem plaidItem,
        string plaidAccountId,
        decimal balance)
    {
        var account = new Account
        {
            Id = Guid.NewGuid(),
            HouseholdId = plaidItem.HouseholdId,
            PlaidItemId = plaidItem.Id,
            PlaidAccountId = plaidAccountId,
            Source = FinancialRecordSource.Plaid,
            Provenance = FinancialRecordProvenance.PlaidSync,
            Name = "Old name",
            Type = AccountTypes.Depository,
            CurrentBalance = balance,
            IsActive = true,
            CreatedAt = SyncedAt.AddDays(-10),
            UpdatedAt = SyncedAt.AddDays(-10)
        };
        dbContext.Accounts.Add(account);
        await dbContext.SaveChangesAsync();
        return account;
    }

    private static async Task<AccountBalanceSnapshot> SeedSnapshotAsync(
        CarduiDBContext dbContext,
        Guid accountId,
        DateOnly date,
        decimal balance)
    {
        var snapshot = new AccountBalanceSnapshot
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Date = date,
            CurrentBalance = balance,
            IsoCurrencyCode = "USD",
            CreatedAt = SyncedAt.AddDays(-1)
        };
        dbContext.AccountBalanceSnapshots.Add(snapshot);
        await dbContext.SaveChangesAsync();
        return snapshot;
    }

    private static PlaidAccount CreatePlaidAccount(
        string accountId,
        decimal? current,
        decimal? available,
        string name = "Prime Checking")
    {
        return new PlaidAccount
        {
            AccountId = accountId,
            Name = name,
            OfficialName = name + " Official",
            Type = Going.Plaid.Entity.AccountType.Depository,
            Subtype = Going.Plaid.Entity.AccountSubtype.Checking,
            Mask = "1234",
            Balances = new Going.Plaid.Entity.AccountBalance
            {
                Current = current,
                Available = available,
                IsoCurrencyCode = "USD"
            }
        };
    }

    private sealed class ScriptedAccountsClient : IPlaidAccountsClient
    {
        private readonly IReadOnlyList<PlaidAccount> _accounts;

        public ScriptedAccountsClient(params PlaidAccount[] accounts)
        {
            _accounts = accounts;
        }

        public List<string> AccessTokens { get; } = [];

        /// <summary>
        /// Returns the scripted accounts and records the unprotected token.
        /// </summary>
        public Task<IReadOnlyList<PlaidAccount>> GetAccountsAsync(
            string accessToken,
            CancellationToken cancellationToken = default)
        {
            AccessTokens.Add(accessToken);
            return Task.FromResult(_accounts);
        }
    }

    private sealed class MappingAccessTokenProtector : IPlaidAccessTokenProtector
    {
        public string Protect(string accessToken) => accessToken;

        /// <summary>
        /// Returns the plain token for the stored test value.
        /// </summary>
        public string Unprotect(string storedAccessToken)
        {
            return storedAccessToken == "stored-token"
                ? "plain-token"
                : storedAccessToken;
        }
    }
}
