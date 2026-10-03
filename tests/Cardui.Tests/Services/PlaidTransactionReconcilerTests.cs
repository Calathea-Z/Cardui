using Cardui.Api.Data;
using Cardui.Api.Models;
using Cardui.Api.Services.Interfaces;
using Cardui.Api.Services.Plaid;
using Going.Plaid.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using PlaidTransaction = Going.Plaid.Entity.Transaction;
using StoredAccount = Cardui.Api.Models.Account;
using StoredTransaction = Cardui.Api.Models.Transaction;

namespace Cardui.Tests.Services;

public class PlaidTransactionReconcilerTests
{
    private static readonly DateTimeOffset SyncedAt =
        new(2026, 10, 2, 20, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Reconcile_RepeatedAddedTransaction_DoesNotCreateDuplicate()
    {
        await using var dbContext = CreateDbContext();
        var (plaidItemId, account) = await SeedAccountAsync(dbContext);
        var reconciler = CreateReconciler(dbContext);
        var transaction = CreatePlaidTransaction("posted-1");

        await reconciler.ReconcileAsync(
            plaidItemId,
            Accounts(account),
            [transaction],
            [],
            [],
            SyncedAt);
        var originalId = (await dbContext.Transactions.SingleAsync()).Id;

        await reconciler.ReconcileAsync(
            plaidItemId,
            Accounts(account),
            [transaction],
            [],
            [],
            SyncedAt.AddMinutes(1));

        var stored = await dbContext.Transactions.SingleAsync();
        Assert.Equal(originalId, stored.Id);
        Assert.Equal("posted-1", stored.PlaidTransactionId);
    }

    [Fact]
    public async Task Reconcile_PendingToPosted_PromotesRowAndPreservesUserEdits()
    {
        await using var dbContext = CreateDbContext();
        var (plaidItemId, account) = await SeedAccountAsync(dbContext);
        var originalId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var userDate = new DateOnly(2026, 9, 28);

        dbContext.Transactions.Add(new StoredTransaction
        {
            Id = originalId,
            AccountId = account.Id,
            PlaidTransactionId = "pending-1",
            Date = userDate,
            IsDateUserEdited = true,
            Name = "Pending merchant",
            Amount = 19.50m,
            IsoCurrencyCode = "USD",
            Pending = true,
            CategoryId = categoryId,
            IsCategoryUserEdited = true,
            Notes = "Keep this note",
            CreatedAt = SyncedAt.AddDays(-2),
            UpdatedAt = SyncedAt.AddDays(-1)
        });
        await dbContext.SaveChangesAsync();

        var posted = CreatePlaidTransaction(
            "posted-1",
            pendingTransactionId: "pending-1");

        var result = await CreateReconciler(dbContext).ReconcileAsync(
            plaidItemId,
            Accounts(account),
            [posted],
            [],
            [new RemovedTransaction { TransactionId = "pending-1" }],
            SyncedAt);

        var stored = await dbContext.Transactions.SingleAsync();
        Assert.Equal(originalId, stored.Id);
        Assert.Equal("posted-1", stored.PlaidTransactionId);
        Assert.False(stored.Pending);
        Assert.Equal(userDate, stored.Date);
        Assert.Equal(categoryId, stored.CategoryId);
        Assert.Equal("Keep this note", stored.Notes);
        Assert.Equal(0, result.Added);
        Assert.Equal(1, result.Modified);
        Assert.Equal(0, result.Removed);
    }

    [Fact]
    public async Task Reconcile_ModifiedTransaction_PreservesUserEditedFields()
    {
        await using var dbContext = CreateDbContext();
        var (plaidItemId, account) = await SeedAccountAsync(dbContext);
        var categoryId = Guid.NewGuid();
        var userDate = new DateOnly(2026, 9, 25);

        dbContext.Transactions.Add(new StoredTransaction
        {
            Id = Guid.NewGuid(),
            AccountId = account.Id,
            PlaidTransactionId = "posted-1",
            Date = userDate,
            IsDateUserEdited = true,
            Name = "Old name",
            Amount = 10m,
            Pending = false,
            CategoryId = categoryId,
            IsCategoryUserEdited = true,
            Notes = "User note",
            CreatedAt = SyncedAt.AddDays(-2),
            UpdatedAt = SyncedAt.AddDays(-1)
        });
        await dbContext.SaveChangesAsync();

        var modified = CreatePlaidTransaction(
            "posted-1",
            date: new DateOnly(2026, 10, 2),
            description: "Updated source name",
            amount: 25m);

        await CreateReconciler(dbContext).ReconcileAsync(
            plaidItemId,
            Accounts(account),
            [],
            [modified],
            [],
            SyncedAt);

        var stored = await dbContext.Transactions.SingleAsync();
        Assert.Equal(userDate, stored.Date);
        Assert.Equal(categoryId, stored.CategoryId);
        Assert.Equal("User note", stored.Notes);
        Assert.Equal("Updated source name", stored.Name);
        Assert.Equal(25m, stored.Amount);
    }

    [Fact]
    public async Task Reconcile_ModifiedTransaction_PreservesExplicitUncategorizedChoice()
    {
        await using var dbContext = CreateDbContext();
        var (plaidItemId, account) = await SeedAccountAsync(dbContext);
        dbContext.Transactions.Add(new StoredTransaction
        {
            Id = Guid.NewGuid(),
            AccountId = account.Id,
            PlaidTransactionId = "posted-1",
            Date = new DateOnly(2026, 10, 1),
            Name = "Merchant",
            Amount = 12.34m,
            Pending = false,
            CategoryId = null,
            IsCategoryUserEdited = true,
            CreatedAt = SyncedAt.AddDays(-1),
            UpdatedAt = SyncedAt.AddDays(-1)
        });
        await dbContext.SaveChangesAsync();

        await CreateReconciler(dbContext, Guid.NewGuid()).ReconcileAsync(
            plaidItemId,
            Accounts(account),
            [],
            [CreatePlaidTransaction("posted-1")],
            [],
            SyncedAt);

        Assert.Null((await dbContext.Transactions.SingleAsync()).CategoryId);
    }

    [Fact]
    public async Task Reconcile_RemovedTransaction_DeletesStoredRow()
    {
        await using var dbContext = CreateDbContext();
        var (plaidItemId, account) = await SeedAccountAsync(dbContext);
        dbContext.Transactions.Add(new StoredTransaction
        {
            Id = Guid.NewGuid(),
            AccountId = account.Id,
            PlaidTransactionId = "removed-1",
            Date = new DateOnly(2026, 10, 1),
            Name = "Removed transaction",
            Amount = 5m,
            Pending = false,
            CreatedAt = SyncedAt.AddDays(-1),
            UpdatedAt = SyncedAt.AddDays(-1)
        });
        await dbContext.SaveChangesAsync();

        var result = await CreateReconciler(dbContext).ReconcileAsync(
            plaidItemId,
            Accounts(account),
            [],
            [],
            [new RemovedTransaction { TransactionId = "removed-1" }],
            SyncedAt);

        Assert.Empty(await dbContext.Transactions.ToListAsync());
        Assert.Equal(1, result.Removed);
    }

    private static PlaidTransactionReconciler CreateReconciler(
        CarduiDBContext dbContext,
        Guid? automaticCategoryId = null) =>
        new(
            dbContext,
            new StubCategorizationService(automaticCategoryId),
            NullLogger<PlaidTransactionReconciler>.Instance);

    private static IReadOnlyDictionary<string, StoredAccount> Accounts(StoredAccount account) =>
        new Dictionary<string, StoredAccount>
        {
            [account.PlaidAccountId] = account
        };

    private static PlaidTransaction CreatePlaidTransaction(
        string transactionId,
        string? pendingTransactionId = null,
        DateOnly? date = null,
        string description = "Merchant",
        decimal amount = 12.34m) =>
        new()
        {
            TransactionId = transactionId,
            PendingTransactionId = pendingTransactionId,
            AccountId = "account-1",
            Date = date ?? new DateOnly(2026, 10, 1),
            OriginalDescription = description,
            Amount = amount,
            IsoCurrencyCode = "USD",
            Pending = false
        };

    private static CarduiDBContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CarduiDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new CarduiDBContext(options);
    }

    private static async Task<(Guid PlaidItemId, StoredAccount Account)> SeedAccountAsync(
        CarduiDBContext dbContext)
    {
        var plaidItemId = Guid.NewGuid();
        var account = new StoredAccount
        {
            Id = Guid.NewGuid(),
            PlaidItemId = plaidItemId,
            PlaidAccountId = "account-1",
            Name = "Checking",
            Type = "depository",
            CreatedAt = SyncedAt.AddDays(-10),
            UpdatedAt = SyncedAt.AddDays(-10)
        };

        dbContext.PlaidItems.Add(new PlaidItem
        {
            Id = plaidItemId,
            PlaidItemId = "item-1",
            AccessToken = "not-a-real-token",
            CreatedAt = SyncedAt.AddDays(-10),
            UpdatedAt = SyncedAt.AddDays(-10)
        });
        dbContext.Accounts.Add(account);
        await dbContext.SaveChangesAsync();

        return (plaidItemId, account);
    }

    private sealed class StubCategorizationService : ITransactionCategorizationService
    {
        private readonly Guid? _categoryId;

        public StubCategorizationService(Guid? categoryId)
        {
            _categoryId = categoryId;
        }

        public Task<Guid?> GetCategoryIdForPlaidTransactionAsync(
            PlaidTransaction transaction,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_categoryId);

        public Task<Guid?> GetCategoryIdForStoredTransactionAsync(
            string name,
            string? merchantName,
            decimal amount,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Guid?>(null);
    }
}
