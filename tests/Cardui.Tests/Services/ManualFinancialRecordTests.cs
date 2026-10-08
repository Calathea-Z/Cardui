using Cardui.Api.Data;
using Cardui.Api.Domain;
using Cardui.Api.Domain.Accounts;
using Cardui.Api.Domain.Categories;
using Cardui.Api.Dtos.Account;
using Cardui.Api.Dtos.Transaction;
using Cardui.Api.Exceptions;
using Cardui.Api.Models;
using Cardui.Api.Security;
using Cardui.Api.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Cardui.Tests.Services;

public class ManualFinancialRecordTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 10, 3, 15, 0, 0, TimeSpan.Zero);

    private static readonly Guid HouseholdA =
        Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    private static readonly Guid HouseholdB =
        Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public async Task GetCashPosition_NamesTheManualSourceAndOldestBalanceDate()
    {
        await using var dbContext = CreateDbContext();
        var scope = Bind(HouseholdA);
        var time = new FakeTimeProvider(Now);
        var accounts = new AccountsService(dbContext, time, scope);
        await accounts.CreateManualAccountAsync(new CreateManualAccountDto
        {
            Name = "Checking",
            Type = "Depository",
            OpeningBalance = 500m,
            OpeningBalanceDate = new DateOnly(2026, 10, 1)
        });

        var position = await accounts.GetCashPositionAsync();

        Assert.Equal(500m, position.Total);
        Assert.Equal(1, position.AccountCount);
        Assert.Equal(1, position.ManualAccountCount);
        Assert.Equal(0, position.ConnectedAccountCount);
        Assert.Equal(new DateOnly(2026, 10, 3), position.OldestBalanceAsOf);
        Assert.Equal(0, position.UnknownBalanceDateCount);
        Assert.Equal(0, position.StaleConnectedAccountCount);
    }

    [Fact]
    public async Task CreateManualAccount_KeepsTheOpeningBalanceOutOfIncome()
    {
        await using var dbContext = CreateDbContext();
        var incomeCategoryId = await SeedIncomeCategoryAsync(dbContext);
        var scope = Bind(HouseholdA);
        var time = new FakeTimeProvider(Now);
        var accounts = new AccountsService(dbContext, time, scope);
        var transactions = new TransactionsService(dbContext, time, scope);
        var dashboard = new DashboardService(dbContext, time, scope);

        var created = await accounts.CreateManualAccountAsync(new CreateManualAccountDto
        {
            Name = "  Cash wallet  ",
            Type = "Depository",
            OpeningBalance = 5_000m,
            OpeningBalanceDate = new DateOnly(2026, 10, 1)
        });

        var beforeActivity = await dashboard.GetSummaryAsync();
        Assert.Equal(5_000m, created.CurrentBalance);
        Assert.Equal(5_000m, created.OpeningBalance);
        Assert.Equal(FinancialRecordSource.Manual, created.Source);
        Assert.Equal(FinancialRecordProvenance.ManualEntry, created.Provenance);
        Assert.Null(created.PlaidItemId);
        Assert.Equal(HouseholdA, await dbContext.Accounts.Select(x => x.HouseholdId).SingleAsync());
        Assert.Equal(0m, beforeActivity.MonthlyIncome);
        Assert.Equal(5_000m, beforeActivity.CashBalance);

        var paycheck = await transactions.CreateManualTransactionAsync(new CreateManualTransactionDto
        {
            AccountId = created.Id,
            Date = new DateOnly(2026, 10, 2),
            Name = "Paycheck",
            Amount = -3_000m,
            CategoryId = incomeCategoryId
        });
        var groceries = await transactions.CreateManualTransactionAsync(new CreateManualTransactionDto
        {
            AccountId = created.Id,
            Date = new DateOnly(2026, 10, 3),
            Name = "Groceries",
            Amount = 100m
        });
        await transactions.CreateManualTransactionAsync(new CreateManualTransactionDto
        {
            AccountId = created.Id,
            Date = new DateOnly(2026, 10, 3),
            Name = "Pending hold",
            Amount = 50m,
            Pending = true
        });

        var afterActivity = await dashboard.GetSummaryAsync();
        var wallet = await dbContext.Accounts.SingleAsync(x => x.Id == created.Id);
        Assert.Equal(3_000m, afterActivity.MonthlyIncome);
        Assert.Equal(100m, afterActivity.MonthlySpending);
        Assert.Equal(7_900m, wallet.CurrentBalance);
        Assert.Equal(FinancialRecordProvenance.ManualEntry, paycheck.Provenance);

        var reconciled = await accounts.ReconcileBalanceAsync(created.Id, new ReconcileAccountBalanceDto
        {
            AsOfDate = new DateOnly(2026, 10, 3),
            StatementBalance = 8_000m
        });

        var afterReconcile = await dashboard.GetSummaryAsync();
        Assert.Equal(100m, reconciled.Adjustment);
        Assert.Equal(8_000m, reconciled.Account.CurrentBalance);
        Assert.Equal(3_000m, afterReconcile.MonthlyIncome);
        Assert.Equal(100m, afterReconcile.MonthlySpending);
        Assert.Equal(
            FinancialRecordProvenance.BalanceReconciliation,
            await dbContext.Transactions
                .Where(x => x.Id == reconciled.AdjustmentTransactionId)
                .Select(x => x.Provenance)
                .SingleAsync());

        await transactions.ArchiveTransactionAsync(groceries.Id);
        var afterArchive = await dashboard.GetSummaryAsync();
        var listed = await transactions.GetTransactionsAsync(new TransactionQueryDto());
        wallet = await dbContext.Accounts.SingleAsync(x => x.Id == created.Id);
        Assert.Equal(0m, afterArchive.MonthlySpending);
        Assert.Equal(3_000m, afterArchive.MonthlyIncome);
        Assert.Equal(8_100m, wallet.CurrentBalance);
        Assert.DoesNotContain(listed.Items, x => x.Id == groceries.Id);

        await accounts.ArchiveAccountAsync(created.Id);
        var hidden = await dashboard.GetSummaryAsync();
        var summary = await accounts.GetAccountsSummaryAsync();
        Assert.Equal(0m, hidden.CashBalance);
        Assert.Equal(3_000m, hidden.MonthlyIncome);
        Assert.Empty(summary.Groups.SelectMany(x => x.Accounts));
        Assert.Equal(created.Id, Assert.Single(summary.ArchivedAccounts).Id);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            new AccountsService(dbContext, time, Bind(HouseholdB))
                .UpdateManualAccountAsync(created.Id, new UpdateManualAccountDto
                {
                    Name = "Other",
                    Type = AccountTypes.Depository,
                    OpeningBalance = 1m,
                    OpeningBalanceDate = new DateOnly(2026, 10, 1)
                }));
    }

    [Fact]
    public async Task ReconcileBalance_MatchesACreditCardWithoutChangingSpending()
    {
        await using var dbContext = CreateDbContext();
        var scope = Bind(HouseholdA);
        var time = new FakeTimeProvider(Now);
        var accounts = new AccountsService(dbContext, time, scope);
        var transactions = new TransactionsService(dbContext, time, scope);

        var card = await accounts.CreateManualAccountAsync(new CreateManualAccountDto
        {
            Name = "Card",
            Type = AccountTypes.Credit,
            OpeningBalance = 200m,
            OpeningBalanceDate = new DateOnly(2026, 10, 1)
        });
        await transactions.CreateManualTransactionAsync(new CreateManualTransactionDto
        {
            AccountId = card.Id,
            Date = new DateOnly(2026, 10, 2),
            Name = "Store",
            Amount = 40m
        });

        var reconciled = await accounts.ReconcileBalanceAsync(card.Id, new ReconcileAccountBalanceDto
        {
            AsOfDate = new DateOnly(2026, 10, 2),
            StatementBalance = 200m
        });
        var dashboard = await new DashboardService(dbContext, time, scope).GetSummaryAsync();

        Assert.Equal(-40m, reconciled.Adjustment);
        Assert.Equal(200m, reconciled.Account.CurrentBalance);
        Assert.Equal(40m, dashboard.MonthlySpending);
        Assert.Equal(0m, dashboard.MonthlyIncome);
        Assert.Equal(200m, dashboard.CreditCardBalance);
    }

    [Fact]
    public async Task LinkedAccount_RejectsManualEditsAndKeepsTheBankBalance()
    {
        await using var dbContext = CreateDbContext();
        var itemId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        dbContext.PlaidItems.Add(new PlaidItem
        {
            Id = itemId,
            HouseholdId = HouseholdA,
            PlaidItemId = "item-a",
            AccessToken = "not-a-real-token",
            CreatedAt = Now,
            UpdatedAt = Now
        });
        dbContext.Accounts.Add(new Account
        {
            Id = accountId,
            HouseholdId = HouseholdA,
            PlaidItemId = itemId,
            PlaidAccountId = "plaid-account",
            Source = FinancialRecordSource.Plaid,
            Provenance = FinancialRecordProvenance.PlaidSync,
            Name = "Bank checking",
            Type = AccountTypes.Depository,
            CurrentBalance = 425m,
            IsActive = true,
            CreatedAt = Now,
            UpdatedAt = Now
        });
        var transactionId = Guid.NewGuid();
        dbContext.Transactions.Add(new Transaction
        {
            Id = transactionId,
            AccountId = accountId,
            PlaidTransactionId = "plaid-txn",
            Source = FinancialRecordSource.Plaid,
            Provenance = FinancialRecordProvenance.PlaidSync,
            Date = new DateOnly(2026, 10, 2),
            Name = "Bank purchase",
            Amount = 18m,
            CreatedAt = Now,
            UpdatedAt = Now
        });
        await dbContext.SaveChangesAsync();

        var time = new FakeTimeProvider(Now);
        var scope = Bind(HouseholdA);
        var accounts = new AccountsService(dbContext, time, scope);
        var transactions = new TransactionsService(dbContext, time, scope);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            accounts.UpdateManualAccountAsync(accountId, new UpdateManualAccountDto
            {
                Name = "Renamed",
                Type = AccountTypes.Depository,
                OpeningBalance = 1m,
                OpeningBalanceDate = new DateOnly(2026, 10, 1)
            }));
        await Assert.ThrowsAsync<BadRequestException>(() =>
            accounts.ReconcileBalanceAsync(accountId, new ReconcileAccountBalanceDto
            {
                AsOfDate = new DateOnly(2026, 10, 3),
                StatementBalance = 1m
            }));

        await transactions.CreateManualTransactionAsync(new CreateManualTransactionDto
        {
            AccountId = accountId,
            Date = new DateOnly(2026, 10, 3),
            Name = "Cash note",
            Amount = 12m
        });
        await transactions.UpdateTransactionDetailsAsync(transactionId, new UpdateTransactionDetailsDto
        {
            Date = new DateOnly(2026, 10, 2),
            Name = "Changed",
            Amount = 1m,
            Notes = "Kept"
        });

        var account = await dbContext.Accounts.SingleAsync(x => x.Id == accountId);
        var bankTransaction = await dbContext.Transactions.SingleAsync(x => x.Id == transactionId);
        Assert.Equal(425m, account.CurrentBalance);
        Assert.Equal("Bank purchase", bankTransaction.Name);
        Assert.Equal(18m, bankTransaction.Amount);
        Assert.Equal("Kept", bankTransaction.Notes);
    }

    [Fact]
    public async Task CreateManualTransaction_RejectsADateBeforeTheOpeningBalance()
    {
        await using var dbContext = CreateDbContext();
        var accounts = new AccountsService(dbContext, new FakeTimeProvider(Now), Bind(HouseholdA));
        var created = await accounts.CreateManualAccountAsync(new CreateManualAccountDto
        {
            Name = "Cash",
            Type = AccountTypes.Depository,
            OpeningBalance = 10m,
            OpeningBalanceDate = new DateOnly(2026, 10, 2)
        });

        await Assert.ThrowsAsync<BadRequestException>(() =>
            new TransactionsService(dbContext, new FakeTimeProvider(Now), Bind(HouseholdA))
                .CreateManualTransactionAsync(new CreateManualTransactionDto
                {
                    AccountId = created.Id,
                    Date = new DateOnly(2026, 10, 1),
                    Name = "Too early",
                    Amount = 1m
                }));
    }

    [Fact]
    public async Task CreateManualAccount_DefaultsToThePlanningCurrency()
    {
        await using var dbContext = CreateDbContext();
        var scope = new HouseholdScope();
        scope.Bind(HouseholdA, "CAD", HouseholdTime.DefaultTimeZoneId);
        var created = await new AccountsService(
            dbContext,
            new FakeTimeProvider(Now),
            scope).CreateManualAccountAsync(new CreateManualAccountDto
        {
            Name = "Wallet",
            Type = "Depository",
            OpeningBalance = 10m,
            OpeningBalanceDate = new DateOnly(2026, 10, 3)
        });

        Assert.Equal("CAD", created.IsoCurrencyCode);
        Assert.True(created.CountsInPlanningTotals);
    }

    private static HouseholdScope Bind(Guid householdId)
    {
        var scope = new HouseholdScope();
        scope.Bind(householdId);
        return scope;
    }

    private static CarduiDBContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CarduiDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new CarduiDBContext(options);
    }

    private static async Task<Guid> SeedIncomeCategoryAsync(CarduiDBContext dbContext)
    {
        var group = new Group
        {
            Id = Guid.NewGuid(),
            Key = SystemGroupKeys.Income,
            Name = "Income",
            SortOrder = 0,
            CreatedAt = Now,
            UpdatedAt = Now
        };
        var subGroup = new SubGroup
        {
            Id = Guid.NewGuid(),
            GroupId = group.Id,
            Key = "income",
            Name = "Income",
            IsSystem = true,
            SortOrder = 0,
            CreatedAt = Now,
            UpdatedAt = Now
        };
        var categoryId = Guid.NewGuid();
        dbContext.Groups.Add(group);
        dbContext.SubGroups.Add(subGroup);
        dbContext.Categories.Add(new Category
        {
            Id = categoryId,
            SubGroupId = subGroup.Id,
            Key = SystemCategoryKeys.Income,
            Name = "Income",
            IsSystem = true,
            CreatedAt = Now,
            UpdatedAt = Now
        });
        await dbContext.SaveChangesAsync();
        return categoryId;
    }
}
