using Cardui.Api.Data;
using Cardui.Api.Domain.Accounts;
using Cardui.Api.Domain.Plaid;
using Cardui.Api.Dtos.Plaid;
using Cardui.Api.Exceptions;
using Cardui.Api.Models;
using Cardui.Api.Security;
using Cardui.Api.Services.Implementations;
using Cardui.Api.Services.Plaid;
using Going.Plaid;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Xunit;
using PlaidConfig = Cardui.Api.Options.PlaidOptions;
using PlaidAccount = Going.Plaid.Entity.Account;

namespace Cardui.Tests.Services;

public class PlaidItemSyncServiceTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 10, 6, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Sync_WhileOneHoldsTheItem_DoesNotStartAnother()
    {
        await using var dbContext = CreateDbContext(out var options);
        var seeded = await SeedItemAsync(dbContext);
        var account = await SeedAccountAsync(dbContext, seeded.Item, 25m);
        var snapshot = await SeedSnapshotAsync(dbContext, account.Id, 25m);
        seeded.Item.LastSyncStartedAt = Now;
        seeded.Item.LastSyncCompletedAt = Now.AddHours(-2);
        seeded.Item.LastSyncFailedAt = Now.AddHours(-3);
        seeded.Item.LastSyncError = "Earlier failure";
        await dbContext.SaveChangesAsync();
        var accounts = new ScriptedAccountsClient(options, _ => [CreatePlaidAccount(80m)]);
        var transactions = new RecordingTransactionSync();
        var service = CreateService(dbContext, seeded.Scope, accounts, transactions);

        var result = await service.SyncPlaidItemAsync(seeded.Item.Id);

        Assert.True(result.AlreadyRunning);
        Assert.Equal(0, result.Transactions.Added);
        Assert.Equal(0, accounts.Calls);
        Assert.Equal(0, transactions.Calls);
        var stored = await dbContext.PlaidItems.SingleAsync();
        Assert.Equal(Now, stored.LastSyncStartedAt);
        Assert.Equal(Now.AddHours(-2), stored.LastSyncCompletedAt);
        Assert.Equal(Now.AddHours(-3), stored.LastSyncFailedAt);
        Assert.Equal("Earlier failure", stored.LastSyncError);
        var storedSnapshot = await dbContext.AccountBalanceSnapshots.SingleAsync();
        Assert.Equal(snapshot.Id, storedSnapshot.Id);
        Assert.Equal(25m, storedSnapshot.CurrentBalance);
        Assert.Equal(25m, (await dbContext.Accounts.SingleAsync()).CurrentBalance);
    }

    [Fact]
    public async Task Sync_CancelledAfterTheClaim_KeepsTheLastSuccessAndRecordsTheInterruption()
    {
        await using var dbContext = CreateDbContext(out var options);
        var seeded = await SeedItemAsync(dbContext);
        var previousSuccess = Now.AddDays(-1);
        seeded.Item.LastSyncStartedAt = previousSuccess;
        seeded.Item.LastSyncCompletedAt = previousSuccess;
        await dbContext.SaveChangesAsync();
        using var cancellation = new CancellationTokenSource();
        var accounts = new ScriptedAccountsClient(options, _ =>
        {
            cancellation.Cancel();
            throw new OperationCanceledException(cancellation.Token);
        });
        var transactions = new RecordingTransactionSync();
        var service = CreateService(dbContext, seeded.Scope, accounts, transactions);

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            service.SyncPlaidItemAsync(seeded.Item.Id, cancellation.Token));

        Assert.Equal(1, accounts.Calls);
        Assert.Equal(0, transactions.Calls);
        var stored = await dbContext.PlaidItems.AsNoTracking().SingleAsync();
        Assert.Equal(previousSuccess, stored.LastSyncCompletedAt);
        Assert.Equal(PlaidItemSync.InterruptedMessage, stored.LastSyncError);
        Assert.NotNull(stored.LastSyncFailedAt);
        Assert.NotNull(stored.LastSyncStartedAt);
        Assert.True(stored.LastSyncFailedAt >= stored.LastSyncStartedAt);
        Assert.False(PlaidItemSync.HoldsItem(
            stored.LastSyncStartedAt,
            stored.LastSyncCompletedAt,
            stored.LastSyncFailedAt,
            Now));
    }

    [Fact]
    public async Task Sync_AbandonedStart_RecordsTheInterruptionThenALaterSuccessClearsIt()
    {
        await using var dbContext = CreateDbContext(out var options);
        var seeded = await SeedItemAsync(dbContext);
        var previousSuccess = Now.AddDays(-2);
        seeded.Item.LastSyncStartedAt = Now - PlaidItemSync.Lease - TimeSpan.FromMinutes(1);
        seeded.Item.LastSyncCompletedAt = previousSuccess;
        await SeedAccountAsync(dbContext, seeded.Item, 10m);
        await dbContext.SaveChangesAsync();
        PlaidItem? duringFetch = null;
        var accounts = new ScriptedAccountsClient(options, item =>
        {
            duringFetch = item;
            return [CreatePlaidAccount(18m)];
        });
        var transactions = new RecordingTransactionSync { Added = 2 };
        var service = CreateService(dbContext, seeded.Scope, accounts, transactions);

        var result = await service.SyncPlaidItemAsync(seeded.Item.Id);

        Assert.False(result.AlreadyRunning);
        Assert.Equal(2, result.Transactions.Added);
        Assert.NotNull(duringFetch);
        Assert.Equal(previousSuccess, duringFetch.LastSyncCompletedAt);
        Assert.Equal(Now, duringFetch.LastSyncFailedAt);
        Assert.Equal(PlaidItemSync.InterruptedMessage, duringFetch.LastSyncError);
        Assert.True(duringFetch.LastSyncStartedAt > duringFetch.LastSyncFailedAt);

        var stored = await dbContext.PlaidItems.AsNoTracking().SingleAsync();
        Assert.Equal(PlaidItemSync.FinishTime(stored.LastSyncStartedAt, Now), stored.LastSyncCompletedAt);
        Assert.Null(stored.LastSyncFailedAt);
        Assert.Null(stored.LastSyncError);
        Assert.Equal(18m, (await dbContext.Accounts.SingleAsync()).CurrentBalance);
        Assert.False(PlaidItemSync.HoldsItem(
            stored.LastSyncStartedAt,
            stored.LastSyncCompletedAt,
            stored.LastSyncFailedAt,
            Now));

        await service.SyncPlaidItemAsync(seeded.Item.Id);
        Assert.Equal(2, accounts.Calls);
    }

    [Fact]
    public async Task Sync_PlaidFailure_KeepsTheLastSuccess()
    {
        await using var dbContext = CreateDbContext(out var options);
        var seeded = await SeedItemAsync(dbContext);
        var previousSuccess = Now.AddDays(-1);
        seeded.Item.LastSyncStartedAt = previousSuccess;
        seeded.Item.LastSyncCompletedAt = previousSuccess;
        seeded.Item.LastSyncFailedAt = null;
        await dbContext.SaveChangesAsync();
        var accounts = new ScriptedAccountsClient(options, _ =>
            throw new PlaidSyncException("The bank could not be reached."));
        var transactions = new RecordingTransactionSync();
        var service = CreateService(dbContext, seeded.Scope, accounts, transactions);

        var ex = await Assert.ThrowsAsync<PlaidSyncException>(() =>
            service.SyncPlaidItemAsync(seeded.Item.Id));

        Assert.Equal("The bank could not be reached.", ex.Message);
        Assert.Equal(0, transactions.Calls);
        var stored = await dbContext.PlaidItems.AsNoTracking().SingleAsync();
        Assert.Equal(previousSuccess, stored.LastSyncCompletedAt);
        Assert.Equal("The bank could not be reached.", stored.LastSyncError);
        Assert.NotNull(stored.LastSyncFailedAt);
        Assert.True(stored.LastSyncFailedAt >= stored.LastSyncStartedAt);
    }

    private static PlaidService CreateService(
        CarduiDBContext dbContext,
        HouseholdScope scope,
        ScriptedAccountsClient accounts,
        RecordingTransactionSync transactions)
    {
        var time = new FakeTimeProvider(Now);
        var protector = new PassThroughAccessTokenProtector();
        var accountSync = new PlaidAccountSyncService(
            dbContext,
            accounts,
            protector,
            NullLogger<PlaidAccountSyncService>.Instance,
            time);

        return new PlaidService(
            dbContext,
            new UnusedClientSource(),
            Microsoft.Extensions.Options.Options.Create(new PlaidConfig
            {
                ClientId = "test-client",
                Secret = "test-secret",
                Environment = "sandbox"
            }),
            new UnusedRequestExecutor(),
            accountSync,
            transactions,
            protector,
            new PlaidItemRemoval(dbContext, time),
            scope,
            NullLogger<PlaidService>.Instance,
            time,
            null!);
    }

    private static CarduiDBContext CreateDbContext(out DbContextOptions<CarduiDBContext> options)
    {
        options = new DbContextOptionsBuilder<CarduiDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new CarduiDBContext(options);
    }

    private static async Task<SeededItem> SeedItemAsync(CarduiDBContext dbContext)
    {
        var householdId = Guid.NewGuid();
        dbContext.Households.Add(new Household
        {
            Id = householdId,
            OwnerClerkUserId = "owner",
            DisplayName = "Household",
            TimeZoneId = "America/Denver",
            CreatedAt = Now.AddDays(-30),
            UpdatedAt = Now.AddDays(-30)
        });
        var item = new PlaidItem
        {
            Id = Guid.NewGuid(),
            HouseholdId = householdId,
            PlaidItemId = "item-1",
            AccessToken = "stored-token",
            CreatedAt = Now.AddDays(-10),
            UpdatedAt = Now.AddDays(-10)
        };
        dbContext.PlaidItems.Add(item);
        await dbContext.SaveChangesAsync();

        var scope = new HouseholdScope();
        scope.Bind(householdId);
        return new SeededItem(item, scope);
    }

    private static async Task<Account> SeedAccountAsync(
        CarduiDBContext dbContext,
        PlaidItem plaidItem,
        decimal balance)
    {
        var account = new Account
        {
            Id = Guid.NewGuid(),
            HouseholdId = plaidItem.HouseholdId,
            PlaidItemId = plaidItem.Id,
            PlaidAccountId = "acct-checking",
            Source = FinancialRecordSource.Plaid,
            Provenance = FinancialRecordProvenance.PlaidSync,
            Name = "Checking",
            Type = AccountTypes.Depository,
            CurrentBalance = balance,
            IsActive = true,
            CreatedAt = Now.AddDays(-10),
            UpdatedAt = Now.AddDays(-10)
        };
        dbContext.Accounts.Add(account);
        await dbContext.SaveChangesAsync();
        return account;
    }

    private static async Task<AccountBalanceSnapshot> SeedSnapshotAsync(
        CarduiDBContext dbContext,
        Guid accountId,
        decimal balance)
    {
        var snapshot = new AccountBalanceSnapshot
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Date = new DateOnly(2026, 10, 6),
            CurrentBalance = balance,
            IsoCurrencyCode = "USD",
            CreatedAt = Now.AddDays(-1)
        };
        dbContext.AccountBalanceSnapshots.Add(snapshot);
        await dbContext.SaveChangesAsync();
        return snapshot;
    }

    private static PlaidAccount CreatePlaidAccount(decimal current)
    {
        return new PlaidAccount
        {
            AccountId = "acct-checking",
            Name = "Checking",
            Type = Going.Plaid.Entity.AccountType.Depository,
            Balances = new Going.Plaid.Entity.AccountBalance
            {
                Current = current,
                IsoCurrencyCode = "USD"
            }
        };
    }

    private sealed record SeededItem(PlaidItem Item, HouseholdScope Scope);

    private sealed class ScriptedAccountsClient : IPlaidAccountsClient
    {
        private readonly DbContextOptions<CarduiDBContext> _options;
        private readonly Func<PlaidItem, IReadOnlyList<PlaidAccount>> _getAccounts;

        public ScriptedAccountsClient(
            DbContextOptions<CarduiDBContext> options,
            Func<PlaidItem, IReadOnlyList<PlaidAccount>> getAccounts)
        {
            _options = options;
            _getAccounts = getAccounts;
        }

        public int Calls { get; private set; }

        /// <summary>
        /// Reads the stored item, then returns or throws from the script.
        /// The read sees the claim, which is saved before accounts are fetched.
        /// </summary>
        public async Task<IReadOnlyList<PlaidAccount>> GetAccountsAsync(
            string accessToken,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            await using var dbContext = new CarduiDBContext(_options);
            var item = await dbContext.PlaidItems.AsNoTracking().SingleAsync(cancellationToken);
            return _getAccounts(item);
        }
    }

    private sealed class RecordingTransactionSync : IPlaidTransactionSyncService
    {
        public int Calls { get; private set; }

        public int Added { get; init; }

        /// <summary>
        /// Records that transaction sync ran and returns the scripted counts.
        /// </summary>
        public Task<SyncTransactionsResponseDto> SyncTransactionsForPlaidItemAsync(
            PlaidItem plaidItem,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(new SyncTransactionsResponseDto { Added = Added });
        }
    }

    private sealed class PassThroughAccessTokenProtector : IPlaidAccessTokenProtector
    {
        public string Protect(string accessToken) => accessToken;

        public string Unprotect(string storedAccessToken) => storedAccessToken;
    }

    private sealed class UnusedClientSource : IPlaidClientSource
    {
        public PlaidClient GetClient() =>
            throw new InvalidOperationException("Plaid was called.");
    }

    private sealed class UnusedRequestExecutor : IPlaidRequestExecutor
    {
        public TRequest WithCredentials<TRequest>(TRequest request, string? accessToken = null)
            where TRequest : RequestBase =>
            throw new InvalidOperationException("Plaid was called.");

        public Task<TResponse> ExecuteAsync<TResponse>(Func<Task<TResponse>> action)
            where TResponse : ResponseBase =>
            throw new InvalidOperationException("Plaid was called.");
    }
}
