using Cardui.Api.Data;
using Cardui.Api.Domain;
using Cardui.Api.Models;
using Cardui.Api.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Cardui.Tests.Services;

public class DashboardServiceTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 10, 2, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetSummary_UsesCurrentPeriodAndMatchesAccountsNetWorth()
    {
        await using var dbContext = CreateDbContext();
        var (checkingId, incomeCategoryId, transferCategoryId) =
            await SeedFinancialDataAsync(dbContext);

        dbContext.Transactions.AddRange(
            CreateTransaction(
                checkingId,
                "income",
                new DateOnly(2026, 10, 1),
                -3_000m,
                incomeCategoryId),
            CreateTransaction(checkingId, "groceries", new DateOnly(2026, 10, 2), 500m),
            CreateTransaction(
                checkingId,
                "transfer",
                new DateOnly(2026, 10, 2),
                200m,
                transferCategoryId),
            CreateTransaction(checkingId, "refund", new DateOnly(2026, 10, 2), -100m),
            CreateTransaction(
                checkingId,
                "pending-purchase",
                new DateOnly(2026, 10, 2),
                1_000m,
                pending: true),
            CreateTransaction(
                checkingId,
                "pending-income",
                new DateOnly(2026, 10, 2),
                -500m,
                incomeCategoryId,
                pending: true),
            CreateTransaction(checkingId, "last-month", new DateOnly(2026, 9, 30), 100m));
        await dbContext.SaveChangesAsync();

        var timeProvider = new FakeTimeProvider(Now);
        var dashboard = await new DashboardService(
            dbContext,
            timeProvider).GetSummaryAsync();
        var accounts = await new AccountsService(
            dbContext,
            timeProvider).GetAccountsSummaryAsync();

        Assert.Equal(new DateOnly(2026, 10, 1), dashboard.PeriodStart);
        Assert.Equal(new DateOnly(2026, 10, 2), dashboard.PeriodEnd);
        Assert.Equal(1_000m, dashboard.CashBalance);
        Assert.Equal(500m, dashboard.CreditCardBalance);
        Assert.Equal(-5_000m, dashboard.NetWorth);
        Assert.Equal(accounts.NetWorth, dashboard.NetWorth);
        var historyPoint = Assert.Single(accounts.History);
        Assert.Equal(new DateOnly(2026, 10, 2), historyPoint.Date);
        Assert.Equal(3_000m, dashboard.MonthlyIncome);
        Assert.Equal(400m, dashboard.MonthlySpending);
        Assert.Contains(
            dashboard.RecentTransactions,
            transaction => transaction.Name == "pending-purchase" && transaction.Pending);

        var category = Assert.Single(dashboard.SpendingByCategory);
        Assert.Equal("Uncategorized", category.CategoryName);
        Assert.Equal(400m, category.Amount);
    }

    private static CarduiDBContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CarduiDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new CarduiDBContext(options);
    }

    private static async Task<(
        Guid CheckingId,
        Guid IncomeCategoryId,
        Guid TransferCategoryId)>
        SeedFinancialDataAsync(CarduiDBContext dbContext)
    {
        var plaidItemId = Guid.NewGuid();
        var checkingId = Guid.NewGuid();

        dbContext.PlaidItems.Add(new PlaidItem
        {
            Id = plaidItemId,
            PlaidItemId = "item-1",
            AccessToken = "not-a-real-token",
            CreatedAt = Now,
            UpdatedAt = Now
        });

        dbContext.Accounts.AddRange(
            CreateAccount(checkingId, plaidItemId, "checking", AccountTypes.Depository, 1_000m),
            CreateAccount(Guid.NewGuid(), plaidItemId, "brokerage", AccountTypes.Investment, 2_000m),
            CreateAccount(Guid.NewGuid(), plaidItemId, "card", AccountTypes.Credit, 500m),
            CreateAccount(Guid.NewGuid(), plaidItemId, "loan", AccountTypes.Loan, 7_500m),
            CreateAccount(
                Guid.NewGuid(),
                plaidItemId,
                "inactive",
                AccountTypes.Depository,
                10_000m,
                isActive: false));
        dbContext.AccountBalanceSnapshots.AddRange(
            CreateSnapshot(checkingId, new DateOnly(2026, 10, 2), 1_000m),
            CreateSnapshot(checkingId, new DateOnly(2026, 10, 3), 1_000m));

        var incomeGroup = CreateGroup("income", "Income");
        var transfersGroup = CreateGroup("transfers", "Transfers");
        var incomeSubGroup = CreateSubGroup(incomeGroup.Id, "income", "Income");
        var transfersSubGroup = CreateSubGroup(
            transfersGroup.Id,
            "transfers",
            "Transfers");

        dbContext.Groups.AddRange(incomeGroup, transfersGroup);
        dbContext.SubGroups.AddRange(incomeSubGroup, transfersSubGroup);

        var incomeCategoryId = Guid.NewGuid();
        dbContext.Categories.Add(new Category
        {
            Id = incomeCategoryId,
            SubGroupId = incomeSubGroup.Id,
            Key = SystemCategoryKeys.Income,
            Name = "Income",
            IsSystem = true,
            CreatedAt = Now,
            UpdatedAt = Now
        });

        var transferCategoryId = Guid.NewGuid();
        dbContext.Categories.Add(new Category
        {
            Id = transferCategoryId,
            SubGroupId = transfersSubGroup.Id,
            Key = SystemCategoryKeys.Transfers,
            Name = "Transfers",
            IsSystem = true,
            CreatedAt = Now,
            UpdatedAt = Now
        });

        await dbContext.SaveChangesAsync();
        return (checkingId, incomeCategoryId, transferCategoryId);
    }

    private static Group CreateGroup(string key, string name)
    {
        return new Group
        {
            Id = Guid.NewGuid(),
            Key = key,
            Name = name,
            SortOrder = 0,
            CreatedAt = Now,
            UpdatedAt = Now
        };
    }

    private static SubGroup CreateSubGroup(
        Guid groupId,
        string key,
        string name)
    {
        return new SubGroup
        {
            Id = Guid.NewGuid(),
            GroupId = groupId,
            Key = key,
            Name = name,
            IsSystem = true,
            SortOrder = 0,
            CreatedAt = Now,
            UpdatedAt = Now
        };
    }

    private static Account CreateAccount(
        Guid id,
        Guid plaidItemId,
        string name,
        string type,
        decimal currentBalance,
        bool isActive = true)
    {
        return new Account
        {
            Id = id,
            PlaidItemId = plaidItemId,
            PlaidAccountId = $"account-{name}",
            Name = name,
            Type = type,
            CurrentBalance = currentBalance,
            IsActive = isActive,
            CreatedAt = Now,
            UpdatedAt = Now
        };
    }

    private static Transaction CreateTransaction(
        Guid accountId,
        string id,
        DateOnly date,
        decimal amount,
        Guid? categoryId = null,
        bool pending = false)
    {
        return new Transaction
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            PlaidTransactionId = $"transaction-{id}",
            Date = date,
            Name = id,
            Amount = amount,
            CategoryId = categoryId,
            Pending = pending,
            CreatedAt = Now,
            UpdatedAt = Now
        };
    }

    private static AccountBalanceSnapshot CreateSnapshot(
        Guid accountId,
        DateOnly date,
        decimal currentBalance)
    {
        return new AccountBalanceSnapshot
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Date = date,
            CurrentBalance = currentBalance,
            CreatedAt = Now
        };
    }
}
