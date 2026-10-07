using Cardui.Api.Data;
using Cardui.Api.Domain.Accounts;
using Cardui.Api.Domain.Categories;
using Cardui.Api.Dtos.Category;
using Cardui.Api.Dtos.SubGroup;
using Cardui.Api.Exceptions;
using Cardui.Api.Models;
using Cardui.Api.Security;
using Cardui.Api.Services.Implementations;
using Cardui.Api.Services.Plaid;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using PlaidConfig = Cardui.Api.Options.PlaidOptions;

namespace Cardui.Tests.Services;

public class HouseholdFinancialScopeTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 10, 3, 12, 0, 0, TimeSpan.Zero);

    private static readonly Guid HouseholdA =
        Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    private static readonly Guid HouseholdB =
        Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public async Task GetAccounts_RequiresABoundHousehold()
    {
        await using var dbContext = CreateDbContext();
        var service = new AccountsService(dbContext, TimeProvider.System, new HouseholdScope());

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetAccountsAsync());
    }

    [Fact]
    public async Task FinancialReads_ReturnOnlyTheBoundHousehold()
    {
        await using var dbContext = CreateDbContext();
        var accountA = Guid.NewGuid();
        var accountB = Guid.NewGuid();
        var transactionA = Guid.NewGuid();
        var transactionB = Guid.NewGuid();

        dbContext.PlaidItems.AddRange(
            CreateItem(Guid.NewGuid(), HouseholdA, "item-a"),
            CreateItem(Guid.NewGuid(), HouseholdB, "item-b"),
            CreateItem(Guid.NewGuid(), null, "item-unassigned"));
        var itemA = dbContext.PlaidItems.Local.Single(x => x.PlaidItemId == "item-a").Id;
        var itemB = dbContext.PlaidItems.Local.Single(x => x.PlaidItemId == "item-b").Id;
        var itemUnassigned = dbContext.PlaidItems.Local.Single(x => x.PlaidItemId == "item-unassigned").Id;

        dbContext.Accounts.AddRange(
            CreateAccount(accountA, itemA, "a-checking", 100m),
            CreateAccount(accountB, itemB, "b-checking", 9_000m),
            CreateAccount(Guid.NewGuid(), itemUnassigned, "old-checking", 50_000m));
        dbContext.Transactions.AddRange(
            CreateTransaction(transactionA, accountA, "txn-a", "Alpha Market", 12m),
            CreateTransaction(transactionB, accountB, "txn-b", "Beta Market", 40m));
        await dbContext.SaveChangesAsync();

        var scope = Bind(HouseholdA);
        var accounts = new AccountsService(dbContext, TimeProvider.System, scope);
        var transactions = new TransactionsService(dbContext, TimeProvider.System, scope);
        var dashboard = new DashboardService(dbContext, TimeProvider.System, scope);

        var listedAccounts = await accounts.GetAccountsAsync();
        var summary = await dashboard.GetSummaryAsync();

        Assert.Equal(accountA, Assert.Single(listedAccounts).Id);
        Assert.Equal(100m, summary.CashBalance);
        Assert.Equal(transactionA, Assert.Single(summary.RecentTransactions).Id);
        await Assert.ThrowsAsync<NotFoundException>(() =>
            transactions.GetTransactionByIdAsync(transactionB));
    }

    [Fact]
    public async Task FinancialReads_LinkedAccountsFollowThePlaidItem()
    {
        await using var dbContext = CreateDbContext();
        var itemId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        dbContext.PlaidItems.Add(CreateItem(itemId, HouseholdA, "item-a"));
        var account = CreateAccount(accountId, itemId, "checking", 40m);
        account.HouseholdId = HouseholdB;
        dbContext.Accounts.Add(account);
        await dbContext.SaveChangesAsync();

        var listed = await new AccountsService(
            dbContext,
            TimeProvider.System,
            Bind(HouseholdA)).GetAccountsAsync();
        var other = await new AccountsService(
            dbContext,
            TimeProvider.System,
            Bind(HouseholdB)).GetAccountsAsync();

        Assert.Equal(accountId, Assert.Single(listed).Id);
        Assert.Empty(other);
    }

    [Fact]
    public async Task FinancialReads_IncludeAccountsWithNoPlaidLink()
    {
        await using var dbContext = CreateDbContext();
        var accountA = Guid.NewGuid();
        var accountB = Guid.NewGuid();
        var looseAccountId = Guid.NewGuid();
        var transactionA = Guid.NewGuid();

        dbContext.Accounts.AddRange(
            CreateUnlinkedAccount(accountA, HouseholdA, "Cash"),
            CreateUnlinkedAccount(accountB, HouseholdB, "Other cash"),
            CreateUnlinkedAccount(looseAccountId, null, "Loose cash"));
        dbContext.Transactions.Add(new Transaction
        {
            Id = transactionA,
            AccountId = accountA,
            PlaidTransactionId = null,
            Source = FinancialRecordSource.Manual,
            Provenance = FinancialRecordProvenance.ManualEntry,
            Date = new DateOnly(2026, 10, 2),
            Name = "Cash deposit",
            Amount = 20m,
            Pending = false,
            CreatedAt = Now,
            UpdatedAt = Now
        });
        await dbContext.SaveChangesAsync();

        var scope = Bind(HouseholdA);
        var accounts = new AccountsService(dbContext, TimeProvider.System, scope);
        var transactions = new TransactionsService(dbContext, TimeProvider.System, scope);

        var listed = await accounts.GetAccountsAsync();
        var transaction = await transactions.GetTransactionByIdAsync(transactionA);

        Assert.Equal(accountA, Assert.Single(listed).Id);
        Assert.Equal(transactionA, transaction.Id);
        await Assert.ThrowsAsync<NotFoundException>(() =>
            new TransactionsService(dbContext, TimeProvider.System, Bind(HouseholdB))
                .GetTransactionByIdAsync(transactionA));
    }

    [Fact]
    public async Task GetPlaidItems_HidesOtherHouseholdsAndUnassignedItems()
    {
        await using var dbContext = CreateDbContext();
        var itemA = Guid.NewGuid();
        var itemB = Guid.NewGuid();
        dbContext.PlaidItems.AddRange(
            CreateItem(itemA, HouseholdA, "item-a"),
            CreateItem(itemB, HouseholdB, "item-b"),
            CreateItem(Guid.NewGuid(), null, "item-unassigned"));
        await dbContext.SaveChangesAsync();

        var service = CreatePlaidService(dbContext, Bind(HouseholdA));

        var items = await service.GetPlaidItemsAsync();

        Assert.Equal(itemA, Assert.Single(items).Id);
        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.SyncPlaidItemAsync(itemB));
    }

    [Fact]
    public async Task CustomCategoriesAndSubGroups_StayInsideTheHousehold()
    {
        await using var dbContext = CreateDbContext();
        var groupId = Guid.NewGuid();
        var systemSubGroupId = Guid.NewGuid();
        var systemCategoryId = Guid.NewGuid();
        dbContext.Groups.Add(CreateGroup(groupId));
        await dbContext.SaveChangesAsync();

        var scope = Bind(HouseholdA);
        var subGroups = new SubGroupsService(dbContext, TimeProvider.System, scope);
        var createdSubGroup = await subGroups.CreateSubGroupAsync(new CreateSubGroupDto
        {
            GroupId = groupId,
            Name = "Pets"
        });

        var storedSubGroup = await dbContext.SubGroups.SingleAsync(x => x.Id == createdSubGroup.Id);
        Assert.Equal(HouseholdA, storedSubGroup.HouseholdId);

        dbContext.SubGroups.Add(CreateSubGroup(
            systemSubGroupId,
            groupId,
            "shopping",
            "Shopping",
            isSystem: true,
            householdId: null));
        dbContext.Categories.Add(CreateCategory(
            systemCategoryId,
            systemSubGroupId,
            "shopping",
            "Shopping",
            isSystem: true,
            householdId: null));
        var customCategoryId = Guid.NewGuid();
        dbContext.Categories.Add(CreateCategory(
            customCategoryId,
            systemSubGroupId,
            "pet-care",
            "Pet Care",
            isSystem: false,
            householdId: HouseholdA));
        await dbContext.SaveChangesAsync();

        var categories = new CategoriesService(dbContext, TimeProvider.System, scope);
        var groups = new GroupsService(dbContext, scope);
        scope.Bind(HouseholdB);

        var visibleCategories = await categories.GetCategoriesAsync();
        var visibleSubGroups = await subGroups.GetSubGroupsAsync(groupId);
        var group = await groups.GetGroupByIdAsync(groupId);

        Assert.Contains(visibleCategories, x => x.Id == systemCategoryId);
        Assert.DoesNotContain(visibleCategories, x => x.Id == customCategoryId);
        Assert.Contains(visibleSubGroups, x => x.Id == systemSubGroupId);
        Assert.DoesNotContain(visibleSubGroups, x => x.Id == createdSubGroup.Id);
        Assert.DoesNotContain(group.SubGroups, x => x.Id == createdSubGroup.Id);
        await Assert.ThrowsAsync<NotFoundException>(() =>
            categories.GetCategoryByIdAsync(customCategoryId));
    }

    [Fact]
    public async Task DeleteCategory_IsRejectedForAnotherHousehold()
    {
        await using var dbContext = CreateDbContext();
        var accountA = Guid.NewGuid();
        var transactionA = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var subGroupId = Guid.NewGuid();
        var groupId = Guid.NewGuid();

        dbContext.Groups.Add(CreateGroup(groupId));
        dbContext.SubGroups.Add(CreateSubGroup(
            subGroupId,
            groupId,
            "pets",
            "Pets",
            isSystem: false,
            householdId: HouseholdA));
        dbContext.Categories.Add(CreateCategory(
            categoryId,
            subGroupId,
            "pet-care",
            "Pet Care",
            isSystem: false,
            householdId: HouseholdA));
        var itemA = Guid.NewGuid();
        dbContext.PlaidItems.Add(CreateItem(itemA, HouseholdA, "item-a"));
        dbContext.Accounts.Add(CreateAccount(accountA, itemA, "a-checking", 10m));
        dbContext.Transactions.Add(CreateTransaction(
            transactionA,
            accountA,
            "txn-a",
            "Vet",
            12m,
            categoryId));
        await dbContext.SaveChangesAsync();

        var scope = Bind(HouseholdA);
        var categories = new CategoriesService(dbContext, TimeProvider.System, scope);
        scope.Bind(HouseholdB);
        await Assert.ThrowsAsync<NotFoundException>(() =>
            categories.DeleteCategoryAsync(categoryId));

        scope.Bind(HouseholdA);
        await categories.DeleteCategoryAsync(categoryId);

        var storedA = await dbContext.Transactions.SingleAsync(x => x.Id == transactionA);
        Assert.Null(storedA.CategoryId);
        Assert.False(await dbContext.Categories.AnyAsync(x => x.Id == categoryId));
    }

    [Fact]
    public async Task UpdateCategory_RejectsSystemCategory()
    {
        await using var dbContext = CreateDbContext();
        var groupId = Guid.NewGuid();
        var subGroupId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        dbContext.Groups.Add(CreateGroup(groupId));
        dbContext.SubGroups.Add(CreateSubGroup(
            subGroupId,
            groupId,
            "shopping",
            "Shopping",
            isSystem: true,
            householdId: null));
        dbContext.Categories.Add(CreateCategory(
            categoryId,
            subGroupId,
            "shopping",
            "Shopping",
            isSystem: true,
            householdId: null));
        await dbContext.SaveChangesAsync();

        var categories = new CategoriesService(
            dbContext,
            TimeProvider.System,
            Bind(HouseholdA));

        await Assert.ThrowsAsync<BadRequestException>(() =>
            categories.UpdateCategoryAsync(categoryId, new UpdateCategoryDto
            {
                Name = "Renamed",
                SubGroupId = subGroupId
            }));

        Assert.Equal("Shopping", (await dbContext.Categories.SingleAsync(x => x.Id == categoryId)).Name);
    }

    [Fact]
    public async Task CreateCategory_AllowsANameUsedByAnotherHousehold()
    {
        await using var dbContext = CreateDbContext();
        var groupId = Guid.NewGuid();
        var subGroupId = Guid.NewGuid();
        dbContext.Groups.Add(CreateGroup(groupId));
        dbContext.SubGroups.Add(CreateSubGroup(
            subGroupId,
            groupId,
            "shopping",
            "Shopping",
            isSystem: true,
            householdId: null));
        dbContext.Categories.Add(CreateCategory(
            Guid.NewGuid(),
            subGroupId,
            "pet-care",
            "Pet Care",
            isSystem: false,
            householdId: HouseholdA));
        await dbContext.SaveChangesAsync();

        var scope = Bind(HouseholdB);
        var categories = new CategoriesService(dbContext, TimeProvider.System, scope);
        var created = await categories.CreateCategoryAsync(new CreateCategoryDto
        {
            Name = "Pet Care",
            SubGroupId = subGroupId
        });

        Assert.Equal(HouseholdB, (await dbContext.Categories.SingleAsync(x => x.Id == created.Id)).HouseholdId);
    }

    [Fact]
    public async Task TransferPairing_DoesNotCrossHouseholds()
    {
        await using var dbContext = CreateDbContext();
        var transfersCategoryId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var subGroupId = Guid.NewGuid();
        dbContext.Groups.Add(CreateGroup(groupId));
        dbContext.SubGroups.Add(CreateSubGroup(
            subGroupId,
            groupId,
            "transfers",
            "Transfers",
            isSystem: true,
            householdId: null));
        dbContext.Categories.Add(CreateCategory(
            transfersCategoryId,
            subGroupId,
            SystemCategoryKeys.Transfers,
            "Transfers",
            isSystem: true,
            householdId: null));

        var itemA = Guid.NewGuid();
        var itemB = Guid.NewGuid();
        var itemOld = Guid.NewGuid();
        dbContext.PlaidItems.AddRange(
            CreateItem(itemA, HouseholdA, "item-a"),
            CreateItem(itemB, HouseholdB, "item-b"),
            CreateItem(itemOld, null, "item-old"));

        var outflowA = CreateAccount(Guid.NewGuid(), itemA, "a-out", 100m);
        var inflowA = CreateAccount(Guid.NewGuid(), itemA, "a-in", 100m);
        var outflowB = CreateAccount(Guid.NewGuid(), itemB, "b-out", 100m);
        var inflowB = CreateAccount(Guid.NewGuid(), itemB, "b-in", 100m);
        var outflowOld = CreateAccount(Guid.NewGuid(), itemOld, "old-out", 100m);
        var inflowOld = CreateAccount(Guid.NewGuid(), itemOld, "old-in", 100m);
        dbContext.Accounts.AddRange(outflowA, inflowA, outflowB, inflowB, outflowOld, inflowOld);

        var pairedA = CreateTransaction(Guid.NewGuid(), outflowA.Id, "a-out-txn", "Zelle to savings", 25m);
        var pairedAIn = CreateTransaction(Guid.NewGuid(), inflowA.Id, "a-in-txn", "Zelle from checking", -25m);
        var pairedB = CreateTransaction(Guid.NewGuid(), outflowB.Id, "b-out-txn", "Zelle to savings", 25m);
        var pairedBIn = CreateTransaction(Guid.NewGuid(), inflowB.Id, "b-in-txn", "Zelle from checking", -25m);
        var pairedOld = CreateTransaction(Guid.NewGuid(), outflowOld.Id, "old-out-txn", "Zelle to savings", 25m);
        var pairedOldIn = CreateTransaction(Guid.NewGuid(), inflowOld.Id, "old-in-txn", "Zelle from checking", -25m);
        dbContext.Transactions.AddRange(pairedA, pairedAIn, pairedB, pairedBIn, pairedOld, pairedOldIn);
        await dbContext.SaveChangesAsync();

        var pairing = new TransferPairingService(
            dbContext,
            new TransactionCategorizationService(dbContext),
            TimeProvider.System,
            Bind(HouseholdA),
            NullLogger<TransferPairingService>.Instance);

        var pairCount = await pairing.PairOwnedAccountTransfersAsync();

        Assert.Equal(1, pairCount);
        Assert.Equal(transfersCategoryId, (await dbContext.Transactions.SingleAsync(x => x.Id == pairedA.Id)).CategoryId);
        Assert.Equal(transfersCategoryId, (await dbContext.Transactions.SingleAsync(x => x.Id == pairedAIn.Id)).CategoryId);
        Assert.Null((await dbContext.Transactions.SingleAsync(x => x.Id == pairedB.Id)).CategoryId);
        Assert.Null((await dbContext.Transactions.SingleAsync(x => x.Id == pairedOld.Id)).CategoryId);
    }

    private static HouseholdScope Bind(Guid householdId)
    {
        var scope = new HouseholdScope();
        scope.Bind(householdId);
        return scope;
    }

    private static PlaidService CreatePlaidService(CarduiDBContext dbContext, HouseholdScope scope)
    {
        return new PlaidService(
            dbContext,
            null!,
            Microsoft.Extensions.Options.Options.Create(new PlaidConfig
            {
                ClientId = "test-client",
                Secret = "test-secret",
                Environment = "sandbox"
            }),
            null!,
            null!,
            null!,
            null!,
            new PlaidItemRemoval(dbContext, TimeProvider.System),
            scope,
            NullLogger<PlaidService>.Instance,
            TimeProvider.System,
            null!);
    }

    private static CarduiDBContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CarduiDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new CarduiDBContext(options);
    }

    private static PlaidItem CreateItem(Guid id, Guid? householdId, string plaidItemId)
    {
        return new PlaidItem
        {
            Id = id,
            HouseholdId = householdId,
            PlaidItemId = plaidItemId,
            AccessToken = "not-a-real-token",
            CreatedAt = Now,
            UpdatedAt = Now
        };
    }

    private static Account CreateUnlinkedAccount(Guid id, Guid? householdId, string name)
    {
        return new Account
        {
            Id = id,
            HouseholdId = householdId,
            PlaidItemId = null,
            PlaidAccountId = null,
            Source = FinancialRecordSource.Manual,
            Provenance = FinancialRecordProvenance.ManualEntry,
            Name = name,
            Type = AccountTypes.Depository,
            CurrentBalance = 20m,
            IsActive = true,
            CreatedAt = Now,
            UpdatedAt = Now
        };
    }

    private static Account CreateAccount(Guid id, Guid plaidItemId, string name, decimal balance)
    {
        return new Account
        {
            Id = id,
            PlaidItemId = plaidItemId,
            PlaidAccountId = name,
            Name = name,
            Type = AccountTypes.Depository,
            CurrentBalance = balance,
            IsActive = true,
            CreatedAt = Now,
            UpdatedAt = Now
        };
    }

    private static Transaction CreateTransaction(
        Guid id,
        Guid accountId,
        string plaidTransactionId,
        string name,
        decimal amount,
        Guid? categoryId = null)
    {
        return new Transaction
        {
            Id = id,
            AccountId = accountId,
            PlaidTransactionId = plaidTransactionId,
            Date = new DateOnly(2026, 10, 2),
            Name = name,
            Amount = amount,
            Pending = false,
            CategoryId = categoryId,
            CreatedAt = Now,
            UpdatedAt = Now
        };
    }

    private static Group CreateGroup(Guid id)
    {
        return new Group
        {
            Id = id,
            Key = "expenses-" + id.ToString("N")[..8],
            Name = "Expenses " + id.ToString("N")[..8],
            SortOrder = 1,
            CreatedAt = Now,
            UpdatedAt = Now
        };
    }

    private static SubGroup CreateSubGroup(
        Guid id,
        Guid groupId,
        string key,
        string name,
        bool isSystem,
        Guid? householdId)
    {
        return new SubGroup
        {
            Id = id,
            GroupId = groupId,
            Key = key,
            Name = name,
            IsSystem = isSystem,
            HouseholdId = householdId,
            SortOrder = 0,
            CreatedAt = Now,
            UpdatedAt = Now
        };
    }

    private static Category CreateCategory(
        Guid id,
        Guid subGroupId,
        string key,
        string name,
        bool isSystem,
        Guid? householdId)
    {
        return new Category
        {
            Id = id,
            SubGroupId = subGroupId,
            Key = key,
            Name = name,
            IsSystem = isSystem,
            HouseholdId = householdId,
            CreatedAt = Now,
            UpdatedAt = Now
        };
    }
}
