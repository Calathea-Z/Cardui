using Cardui.Api.Data;
using Cardui.Api.Dtos.Transaction;
using Cardui.Api.Exceptions;
using Cardui.Api.Models;
using Cardui.Api.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Cardui.Tests.Services;

public class TransactionsServiceTests
{
    private static readonly DateTimeOffset SeededAt =
        new(2026, 7, 1, 12, 0, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset UpdatedAt =
        new(2026, 7, 20, 18, 30, 0, TimeSpan.Zero);

    [Fact]
    public async Task UpdateTransactionDetails_UpdatesDateCategoryAndNotes()
    {
        await using var dbContext = CreateDbContext();
        var (transactionId, categoryId) = await SeedTransactionAsync(dbContext);

        var timeProvider = new FakeTimeProvider(UpdatedAt);
        var service = new TransactionsService(dbContext, timeProvider);

        var result = await service.UpdateTransactionDetailsAsync(
            transactionId,
            new UpdateTransactionDetailsDto
            {
                Date = new DateOnly(2026, 7, 18),
                CategoryId = categoryId,
                Notes = "Updated note"
            });

        Assert.Equal(new DateOnly(2026, 7, 18), result.Date);
        Assert.Equal(categoryId, result.Category?.Id);
        Assert.Equal("Updated note", result.Notes);
        Assert.Equal(UpdatedAt, (await dbContext.Transactions.SingleAsync()).UpdatedAt);
    }

    [Fact]
    public async Task UpdateTransactionDetails_RejectsMissingTransaction()
    {
        await using var dbContext = CreateDbContext();
        var service = new TransactionsService(dbContext, new FakeTimeProvider(UpdatedAt));

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.UpdateTransactionDetailsAsync(
                Guid.NewGuid(),
                new UpdateTransactionDetailsDto
                {
                    Date = new DateOnly(2026, 7, 18),
                    Notes = "Missing"
                }));
    }

    [Fact]
    public async Task UpdateTransactionDetails_RejectsUnknownCategory()
    {
        await using var dbContext = CreateDbContext();
        var (transactionId, _) = await SeedTransactionAsync(dbContext, includeCategory: false);
        var service = new TransactionsService(dbContext, new FakeTimeProvider(UpdatedAt));

        await Assert.ThrowsAsync<BadRequestException>(() =>
            service.UpdateTransactionDetailsAsync(
                transactionId,
                new UpdateTransactionDetailsDto
                {
                    Date = new DateOnly(2026, 7, 18),
                    CategoryId = Guid.NewGuid(),
                    Notes = "Bad category"
                }));
    }

    [Fact]
    public async Task GetMerchantHistory_MatchesByMerchantName_CaseInsensitive()
    {
        await using var dbContext = CreateDbContext();
        var accountId = await SeedAccountAsync(dbContext);

        var currentId = await SeedNamedTransactionAsync(
            dbContext,
            accountId,
            "txn-current",
            new DateOnly(2026, 7, 20),
            name: "POS CHEWY #123",
            merchantName: "Chewy",
            amount: 40m);

        await SeedNamedTransactionAsync(
            dbContext,
            accountId,
            "txn-older",
            new DateOnly(2026, 6, 1),
            name: "CHEWY.COM",
            merchantName: "chewy",
            amount: 20m);

        await SeedNamedTransactionAsync(
            dbContext,
            accountId,
            "txn-other",
            new DateOnly(2026, 7, 15),
            name: "Starbucks",
            merchantName: "Starbucks",
            amount: 8m);

        var service = new TransactionsService(dbContext, new FakeTimeProvider(UpdatedAt));

        var history = await service.GetMerchantHistoryAsync(currentId, "monthly");

        Assert.Equal("Chewy", history.DisplayName);
        Assert.Equal(2, history.TotalTransactionCount);
        Assert.Equal("2026-07", history.SelectedPeriodKey);
        Assert.Contains(history.Periods, period => period.Key == "2026-06" && period.TotalAmount == 20m);
        Assert.Contains(history.Periods, period => period.Key == "2026-07" && period.TotalAmount == 40m);
        Assert.Equal(2, history.Transactions.Count);
    }

    [Fact]
    public async Task GetMerchantHistory_FallsBackToName_WhenMerchantMissing()
    {
        await using var dbContext = CreateDbContext();
        var accountId = await SeedAccountAsync(dbContext);

        var currentId = await SeedNamedTransactionAsync(
            dbContext,
            accountId,
            "txn-ach-1",
            new DateOnly(2026, 7, 20),
            name: "ACH DEBIT PAYPAL",
            merchantName: null);

        await SeedNamedTransactionAsync(
            dbContext,
            accountId,
            "txn-ach-2",
            new DateOnly(2026, 7, 10),
            name: "ach debit paypal",
            merchantName: null);

        var service = new TransactionsService(dbContext, new FakeTimeProvider(UpdatedAt));

        var history = await service.GetMerchantHistoryAsync(currentId);

        Assert.Equal("ACH DEBIT PAYPAL", history.DisplayName);
        Assert.Equal(2, history.TotalTransactionCount);
        Assert.Equal(2, history.Transactions.Count);
    }

    [Fact]
    public async Task GetMerchantHistory_DefaultsSelectedPeriodToCurrentMonth_WhenEmpty()
    {
        await using var dbContext = CreateDbContext();
        var accountId = await SeedAccountAsync(dbContext);

        var currentId = await SeedNamedTransactionAsync(
            dbContext,
            accountId,
            "txn-june",
            new DateOnly(2026, 6, 10),
            name: "Target",
            merchantName: "Target",
            amount: 15m);

        var service = new TransactionsService(dbContext, new FakeTimeProvider(UpdatedAt));

        var history = await service.GetMerchantHistoryAsync(currentId, "monthly");

        Assert.Equal("2026-07", history.SelectedPeriodKey);
        var july = Assert.Single(history.Periods, period => period.Key == "2026-07");
        Assert.Equal(0, july.TransactionCount);
        Assert.Equal(0m, july.TotalAmount);
    }

    private static CarduiDBContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CarduiDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new CarduiDBContext(options);
    }

    private static async Task<Guid> SeedAccountAsync(CarduiDBContext dbContext)
    {
        var plaidItemId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        dbContext.PlaidItems.Add(new PlaidItem
        {
            Id = plaidItemId,
            PlaidItemId = "item-1",
            AccessToken = "access-token",
            CreatedAt = SeededAt,
            UpdatedAt = SeededAt
        });

        dbContext.Accounts.Add(new Account
        {
            Id = accountId,
            PlaidItemId = plaidItemId,
            PlaidAccountId = "account-1",
            Name = "Checking",
            Type = "depository",
            Subtype = "checking",
            CreatedAt = SeededAt,
            UpdatedAt = SeededAt
        });

        await dbContext.SaveChangesAsync();
        return accountId;
    }

    private static async Task<Guid> SeedNamedTransactionAsync(
        CarduiDBContext dbContext,
        Guid accountId,
        string plaidTransactionId,
        DateOnly date,
        string name,
        string? merchantName,
        decimal amount = 10m)
    {
        var transactionId = Guid.NewGuid();

        dbContext.Transactions.Add(new Transaction
        {
            Id = transactionId,
            AccountId = accountId,
            PlaidTransactionId = plaidTransactionId,
            Date = date,
            Name = name,
            MerchantName = merchantName,
            Amount = amount,
            IsoCurrencyCode = "USD",
            Pending = false,
            CategoryId = null,
            Notes = null,
            CreatedAt = SeededAt,
            UpdatedAt = SeededAt
        });

        await dbContext.SaveChangesAsync();
        return transactionId;
    }

    private static async Task<(Guid TransactionId, Guid CategoryId)> SeedTransactionAsync(
        CarduiDBContext dbContext,
        bool includeCategory = true)
    {
        var plaidItemId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        dbContext.PlaidItems.Add(new PlaidItem
        {
            Id = plaidItemId,
            PlaidItemId = "item-1",
            AccessToken = "access-token",
            CreatedAt = SeededAt,
            UpdatedAt = SeededAt
        });

        dbContext.Accounts.Add(new Account
        {
            Id = accountId,
            PlaidItemId = plaidItemId,
            PlaidAccountId = "account-1",
            Name = "Checking",
            Type = "depository",
            Subtype = "checking",
            CreatedAt = SeededAt,
            UpdatedAt = SeededAt
        });

        if (includeCategory)
        {
            var groupId = Guid.NewGuid();
            var subGroupId = Guid.NewGuid();

            dbContext.Groups.Add(new Group
            {
                Id = groupId,
                Key = "expenses",
                Name = "Expenses",
                SortOrder = 1,
                CreatedAt = SeededAt,
                UpdatedAt = SeededAt
            });

            dbContext.SubGroups.Add(new SubGroup
            {
                Id = subGroupId,
                GroupId = groupId,
                Key = "food-dining",
                Name = "Food & Dining",
                IsSystem = true,
                SortOrder = 0,
                CreatedAt = SeededAt,
                UpdatedAt = SeededAt
            });

            dbContext.Categories.Add(new Category
            {
                Id = categoryId,
                SubGroupId = subGroupId,
                Key = "food-dining",
                Name = "Food & Dining",
                Color = "#f97316",
                IsSystem = true,
                CreatedAt = SeededAt,
                UpdatedAt = SeededAt
            });
        }

        dbContext.Transactions.Add(new Transaction
        {
            Id = transactionId,
            AccountId = accountId,
            PlaidTransactionId = "txn-1",
            Date = new DateOnly(2026, 7, 10),
            Name = "Coffee Shop",
            MerchantName = "Coffee Shop",
            Amount = 5.25m,
            IsoCurrencyCode = "USD",
            Pending = false,
            CategoryId = includeCategory ? categoryId : null,
            Notes = "Original note",
            CreatedAt = SeededAt,
            UpdatedAt = SeededAt
        });

        await dbContext.SaveChangesAsync();

        return (transactionId, categoryId);
    }
}
