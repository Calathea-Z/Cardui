using Cardui.Api.Data;
using Cardui.Api.Dtos.Plaid;
using Cardui.Api.Exceptions;
using Cardui.Api.Models;
using Cardui.Api.Security;
using Cardui.Api.Services.Interfaces;
using Cardui.Api.Services.Plaid;
using Going.Plaid.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Xunit;
using PlaidTransaction = Going.Plaid.Entity.Transaction;
using StoredAccount = Cardui.Api.Models.Account;

namespace Cardui.Tests.Services;

public class PlaidTransactionSyncServiceTests
{
    private static readonly DateTimeOffset SyncedAt =
        new(2026, 10, 3, 16, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Sync_WalksMultiplePages_PersistsAccumulatedTransactionsAndCursor()
    {
        await using var dbContext = CreateDbContext();
        var plaidItem = await SeedPlaidItemAsync(dbContext, cursor: "cursor-start");
        await SeedStoredTransactionAsync(dbContext, plaidItem, "modified-1", 10m);
        await SeedStoredTransactionAsync(dbContext, plaidItem, "removed-1", 5m);
        var pageClient = new ScriptedTransactionPageClient(
            () => new PlaidTransactionPageDto
            {
                Added = [CreatePlaidTransaction("added-1")],
                NextCursor = "cursor-page-1",
                HasMore = true
            },
            () => new PlaidTransactionPageDto
            {
                Added = [CreatePlaidTransaction("added-2")],
                Modified = [CreatePlaidTransaction("modified-1", amount: 40m)],
                Removed = [new RemovedTransaction { TransactionId = "removed-1" }],
                NextCursor = "cursor-final",
                HasMore = false
            });
        var pairing = new RecordingTransferPairingService();
        var service = CreateService(dbContext, pageClient, pairing);

        var result = await service.SyncTransactionsForPlaidItemAsync(plaidItem);

        Assert.Equal(2, result.Added);
        Assert.Equal(1, result.Modified);
        Assert.Equal(1, result.Removed);
        Assert.Equal("cursor-final", result.NextCursor);
        Assert.Equal("cursor-final", plaidItem.TransactionsCursor);
        Assert.Equal(SyncedAt, plaidItem.LastTransactionsSyncedAt);
        Assert.Equal(
            ["cursor-start", "cursor-page-1"],
            pageClient.RequestedCursors);
        Assert.Equal(["unprotected-token"], pageClient.RequestedAccessTokens.Distinct());
        Assert.Equal(1, pairing.CallCount);

        var storedIds = await dbContext.Transactions
            .OrderBy(x => x.PlaidTransactionId)
            .Select(x => x.PlaidTransactionId)
            .ToListAsync();
        Assert.Equal(["added-1", "added-2", "modified-1"], storedIds);
    }

    [Fact]
    public async Task Sync_EmptyFinalPage_StillPersistsCursor()
    {
        await using var dbContext = CreateDbContext();
        var plaidItem = await SeedPlaidItemAsync(dbContext);
        var service = CreateService(
            dbContext,
            new ScriptedTransactionPageClient(
                () => new PlaidTransactionPageDto
                {
                    NextCursor = "cursor-empty",
                    HasMore = false
                }));

        var result = await service.SyncTransactionsForPlaidItemAsync(plaidItem);

        Assert.Equal(0, result.Added);
        Assert.Equal("cursor-empty", result.NextCursor);
        Assert.Equal("cursor-empty", plaidItem.TransactionsCursor);
        Assert.Empty(await dbContext.Transactions.ToListAsync());
    }

    [Fact]
    public async Task Sync_PageRetrievalFails_DoesNotAdvanceCursorOrPersistTransactions()
    {
        await using var dbContext = CreateDbContext();
        var plaidItem = await SeedPlaidItemAsync(dbContext, cursor: "cursor-start");
        var pairing = new RecordingTransferPairingService();
        var service = CreateService(
            dbContext,
            new ScriptedTransactionPageClient(
                () => new PlaidTransactionPageDto
                {
                    Added = [CreatePlaidTransaction("added-1")],
                    NextCursor = "cursor-page-1",
                    HasMore = true
                },
                () => throw new PlaidSyncException("Plaid page retrieval failed.")),
            pairing);

        var ex = await Assert.ThrowsAsync<PlaidSyncException>(
            () => service.SyncTransactionsForPlaidItemAsync(plaidItem));

        Assert.Equal("Plaid page retrieval failed.", ex.Message);
        Assert.Equal("cursor-start", plaidItem.TransactionsCursor);
        Assert.Null(plaidItem.LastTransactionsSyncedAt);
        Assert.Empty(await dbContext.Transactions.ToListAsync());
        Assert.Equal(0, pairing.CallCount);
    }

    [Fact]
    public async Task Sync_CancelledBetweenPages_DoesNotAdvanceCursorOrPersistTransactions()
    {
        await using var dbContext = CreateDbContext();
        var plaidItem = await SeedPlaidItemAsync(dbContext, cursor: "cursor-start");
        using var cancellation = new CancellationTokenSource();
        var pairing = new RecordingTransferPairingService();
        var service = CreateService(
            dbContext,
            new ScriptedTransactionPageClient(
                () =>
                {
                    cancellation.Cancel();
                    return new PlaidTransactionPageDto
                    {
                        Added = [CreatePlaidTransaction("added-1")],
                        NextCursor = "cursor-page-1",
                        HasMore = true
                    };
                },
                () => new PlaidTransactionPageDto
                {
                    Added = [CreatePlaidTransaction("added-2")],
                    NextCursor = "cursor-final",
                    HasMore = false
                }),
            pairing);

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => service.SyncTransactionsForPlaidItemAsync(
                plaidItem,
                cancellation.Token));

        Assert.Equal("cursor-start", plaidItem.TransactionsCursor);
        Assert.Null(plaidItem.LastTransactionsSyncedAt);
        Assert.Empty(await dbContext.Transactions.ToListAsync());
        Assert.Equal(0, pairing.CallCount);
    }

    private static PlaidTransactionSyncService CreateService(
        CarduiDBContext dbContext,
        IPlaidTransactionPageClient pageClient,
        RecordingTransferPairingService? pairing = null)
    {
        pairing ??= new RecordingTransferPairingService();

        return new PlaidTransactionSyncService(
            dbContext,
            new PassThroughAccessTokenProtector(),
            pageClient,
            new PlaidTransactionReconciler(
                dbContext,
                new StubCategorizationService(),
                NullLogger<PlaidTransactionReconciler>.Instance),
            new StubCategorizationService(),
            pairing,
            new FakeTimeProvider(SyncedAt));
    }

    private static CarduiDBContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CarduiDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new CarduiDBContext(options);
    }

    private static async Task<PlaidItem> SeedPlaidItemAsync(
        CarduiDBContext dbContext,
        string? cursor = null)
    {
        var createdAt = SyncedAt.AddDays(-10);
        var plaidItem = new PlaidItem
        {
            Id = Guid.NewGuid(),
            PlaidItemId = "item-1",
            AccessToken = "unprotected-token",
            TransactionsCursor = cursor,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };

        dbContext.PlaidItems.Add(plaidItem);
        dbContext.Accounts.Add(new StoredAccount
        {
            Id = Guid.NewGuid(),
            PlaidItemId = plaidItem.Id,
            PlaidAccountId = "account-1",
            Name = "Checking",
            Type = "depository",
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        });
        await dbContext.SaveChangesAsync();

        return plaidItem;
    }

    private static async Task SeedStoredTransactionAsync(
        CarduiDBContext dbContext,
        PlaidItem plaidItem,
        string plaidTransactionId,
        decimal amount)
    {
        var accountId = await dbContext.Accounts
            .Where(x => x.PlaidItemId == plaidItem.Id)
            .Select(x => x.Id)
            .SingleAsync();

        dbContext.Transactions.Add(new Cardui.Api.Models.Transaction
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            PlaidTransactionId = plaidTransactionId,
            Date = new DateOnly(2026, 9, 30),
            Name = plaidTransactionId,
            Amount = amount,
            IsoCurrencyCode = "USD",
            Pending = false,
            CreatedAt = SyncedAt.AddDays(-2),
            UpdatedAt = SyncedAt.AddDays(-2)
        });
        await dbContext.SaveChangesAsync();
    }

    private static PlaidTransaction CreatePlaidTransaction(
        string transactionId,
        decimal amount = 12.34m) =>
        new()
        {
            TransactionId = transactionId,
            AccountId = "account-1",
            Date = new DateOnly(2026, 10, 1),
            OriginalDescription = transactionId,
            Amount = amount,
            IsoCurrencyCode = "USD",
            Pending = false
        };

    private sealed class ScriptedTransactionPageClient : IPlaidTransactionPageClient
    {
        private readonly Queue<Func<PlaidTransactionPageDto>> _pages;

        public ScriptedTransactionPageClient(params Func<PlaidTransactionPageDto>[] pages)
        {
            _pages = new Queue<Func<PlaidTransactionPageDto>>(pages);
        }

        public List<string> RequestedAccessTokens { get; } = [];

        public List<string?> RequestedCursors { get; } = [];

        public Task<PlaidTransactionPageDto> GetPageAsync(
            string accessToken,
            string? cursor,
            CancellationToken cancellationToken = default)
        {
            RequestedAccessTokens.Add(accessToken);
            RequestedCursors.Add(cursor);

            if (_pages.Count == 0)
            {
                throw new InvalidOperationException("No more scripted Plaid transaction pages.");
            }

            return Task.FromResult(_pages.Dequeue()());
        }
    }

    private sealed class PassThroughAccessTokenProtector : IPlaidAccessTokenProtector
    {
        public string Protect(string accessToken) => accessToken;

        public string Unprotect(string storedAccessToken) => storedAccessToken;
    }

    private sealed class RecordingTransferPairingService : ITransferPairingService
    {
        public int CallCount { get; private set; }

        public Task<int> PairOwnedAccountTransfersAsync(
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(0);
        }
    }

    private sealed class StubCategorizationService : ITransactionCategorizationService
    {
        public Task<Guid?> GetCategoryIdForPlaidTransactionAsync(
            PlaidTransaction transaction,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Guid?>(null);

        public Task<Guid?> GetCategoryIdForStoredTransactionAsync(
            string name,
            string? merchantName,
            decimal amount,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Guid?>(null);

        public Task<IReadOnlyDictionary<string, Guid>> GetSystemCategoryIdsByKeyAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<string, Guid>>(new Dictionary<string, Guid>());

        public Guid? FindCategoryId(
            IReadOnlyDictionary<string, Guid> categoryIdsByKey,
            string name,
            string? merchantName,
            decimal amount) =>
            null;
    }
}
