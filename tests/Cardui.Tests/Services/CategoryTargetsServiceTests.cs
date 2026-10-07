using Cardui.Api.Data;
using Cardui.Api.Domain;
using Cardui.Api.Domain.Accounts;
using Cardui.Api.Domain.Categories;
using Cardui.Api.Dtos.CategoryTargets;
using Cardui.Api.Exceptions;
using Cardui.Api.Models;
using Cardui.Api.Security;
using Cardui.Api.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Cardui.Tests.Services;

public class CategoryTargetsServiceTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 10, 5, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Get_DoesNotSaveACopyOfThePreviousMonth()
    {
        await using var dbContext = CreateDbContext();
        await DataSeeder.SeedAsync(dbContext, new FakeTimeProvider(Now));
        var householdId = await CreateHouseholdAsync(dbContext, "user_owner");
        var groceries = await CategoryAsync(dbContext, SystemCategoryKeys.FoodDining);
        var service = CreateService(dbContext, Bind(householdId));
        await service.SaveAsync(groceries.Id, Target(2026, 9, 120m, rollover: true));

        var october = await service.GetAsync(2026, 10);

        Assert.False(october.Saved);
        Assert.Equal(9, october.CopiedFromMonth);
        var line = october.Categories.Single(item => item.CategoryId == groceries.Id);
        Assert.Equal(120m, line.Target);
        Assert.True(line.Rollover);
        Assert.Equal(1, await dbContext.CategoryTargetMonths.CountAsync());
    }

    [Fact]
    public async Task Save_CopiesTheOtherCategoriesAndLeavesThePriorMonth()
    {
        await using var dbContext = CreateDbContext();
        await DataSeeder.SeedAsync(dbContext, new FakeTimeProvider(Now));
        var householdId = await CreateHouseholdAsync(dbContext, "user_owner");
        var groceries = await CategoryAsync(dbContext, SystemCategoryKeys.FoodDining);
        var shopping = await CategoryAsync(dbContext, SystemCategoryKeys.Shopping);
        var service = CreateService(dbContext, Bind(householdId));
        await service.SaveAsync(groceries.Id, Target(2026, 9, 100m, rollover: true));
        await service.SaveAsync(shopping.Id, Target(2026, 9, 40m, rollover: false));

        var october = await service.SaveAsync(shopping.Id, Target(2026, 10, 25m, rollover: true));

        Assert.True(october.Saved);
        Assert.Equal(9, october.CopiedFromMonth);
        Assert.Equal(25m, Line(october, shopping.Id).Target);
        Assert.True(Line(october, shopping.Id).Rollover);
        Assert.Equal(100m, Line(october, groceries.Id).Target);
        Assert.True(Line(october, groceries.Id).Rollover);

        var september = await service.GetAsync(2026, 9);
        Assert.Equal(40m, Line(september, shopping.Id).Target);
        Assert.False(Line(september, shopping.Id).Rollover);
    }

    [Fact]
    public async Task Save_RejectsIncomeTransfersAndABadAmount()
    {
        await using var dbContext = CreateDbContext();
        await DataSeeder.SeedAsync(dbContext, new FakeTimeProvider(Now));
        var householdId = await CreateHouseholdAsync(dbContext, "user_owner");
        var income = await CategoryAsync(dbContext, SystemCategoryKeys.Income);
        var transfers = await CategoryAsync(dbContext, SystemCategoryKeys.Transfers);
        var groceries = await CategoryAsync(dbContext, SystemCategoryKeys.FoodDining);
        var service = CreateService(dbContext, Bind(householdId));

        var incomeError = await Assert.ThrowsAsync<BadRequestException>(() =>
            service.SaveAsync(income.Id, Target(2026, 10, 10m, false)));
        var transferError = await Assert.ThrowsAsync<BadRequestException>(() =>
            service.SaveAsync(transfers.Id, Target(2026, 10, 10m, false)));
        var amountError = await Assert.ThrowsAsync<BadRequestException>(() =>
            service.SaveAsync(groceries.Id, Target(2026, 10, -5m, false)));

        Assert.Equal(
            "Set a target on a spending category. Income and transfers are not spending.",
            incomeError.Message);
        Assert.Equal(incomeError.Message, transferError.Message);
        Assert.Equal("Enter the target as zero or more.", amountError.Message);

        var zero = await service.SaveAsync(groceries.Id, Target(2026, 10, 0m, false));
        Assert.Equal(0m, Line(zero, groceries.Id).Target);
    }

    [Fact]
    public async Task Spent_UsesPostedActivityRulesAndStopsToday()
    {
        await using var dbContext = CreateDbContext();
        await DataSeeder.SeedAsync(dbContext, new FakeTimeProvider(Now));
        var householdId = await CreateHouseholdAsync(dbContext, "user_owner");
        var accountId = await AddAccountAsync(dbContext, householdId);
        var groceries = await CategoryAsync(dbContext, SystemCategoryKeys.FoodDining);
        var income = await CategoryAsync(dbContext, SystemCategoryKeys.Income);
        var transfers = await CategoryAsync(dbContext, SystemCategoryKeys.Transfers);
        var mortgage = await CategoryAsync(dbContext, SystemCategoryKeys.BillsUtilities);
        await AddTransactionAsync(dbContext, accountId, new DateOnly(2026, 10, 2), -3_000m, income.Id, "Paycheck");
        await AddTransactionAsync(dbContext, accountId, new DateOnly(2026, 10, 3), 80m, groceries.Id, "Groceries");
        await AddTransactionAsync(dbContext, accountId, new DateOnly(2026, 10, 4), -30m, groceries.Id, "Groceries refund");
        await AddTransactionAsync(dbContext, accountId, new DateOnly(2026, 10, 1), 900m, transfers.Id, "Credit card payment");
        await AddTransactionAsync(dbContext, accountId, new DateOnly(2026, 10, 1), -900m, transfers.Id, "Credit card payment");
        await AddTransactionAsync(
            dbContext,
            accountId,
            new DateOnly(2026, 10, 2),
            75m,
            groceries.Id,
            "Pending groceries",
            pending: true);
        await AddTransactionAsync(
            dbContext,
            accountId,
            new DateOnly(2026, 10, 2),
            40m,
            groceries.Id,
            "Statement match",
            provenance: FinancialRecordProvenance.BalanceReconciliation);
        await AddTransactionAsync(
            dbContext,
            accountId,
            new DateOnly(2026, 10, 2),
            25m,
            groceries.Id,
            "Euro groceries",
            currency: "EUR");
        await AddTransactionAsync(dbContext, accountId, new DateOnly(2026, 10, 2), 15m, null, "Uncategorized");
        await AddTransactionAsync(dbContext, accountId, new DateOnly(2026, 10, 3), 400m, mortgage.Id, "Mortgage");
        await AddTransactionAsync(dbContext, accountId, new DateOnly(2026, 10, 6), 999m, groceries.Id, "Later groceries");
        var service = CreateService(dbContext, Bind(householdId));

        var october = await service.GetAsync(2026, 10);

        Assert.True(october.ThroughToday);
        Assert.Equal(465m, october.Spent);
        Assert.Equal(465m, october.OtherSpent);
        Assert.Null(october.Remaining);
        Assert.Equal(50m, Line(october, groceries.Id).Spent);
        Assert.Equal(400m, Line(october, mortgage.Id).Spent);
        Assert.Equal(15m, october.Categories.Single(item => item.CategoryId == null).Spent);
        Assert.DoesNotContain(october.Categories, item => item.CategoryId == income.Id);
        Assert.DoesNotContain(october.Categories, item => item.CategoryId == transfers.Id);
        Assert.Equal(1, october.ExcludedTransactionCount);
        Assert.Equal("EUR", Assert.Single(october.ExcludedCurrencies));
    }

    [Fact]
    public async Task Rollover_ComesFromThePreviousMonthWhenThatMonthTurnedItOn()
    {
        await using var dbContext = CreateDbContext();
        await DataSeeder.SeedAsync(dbContext, new FakeTimeProvider(Now));
        var householdId = await CreateHouseholdAsync(dbContext, "user_owner");
        var accountId = await AddAccountAsync(dbContext, householdId);
        var groceries = await CategoryAsync(dbContext, SystemCategoryKeys.FoodDining);
        await AddTransactionAsync(
            dbContext,
            accountId,
            new DateOnly(2026, 9, 12),
            40m,
            groceries.Id,
            "Groceries");
        var service = CreateService(dbContext, Bind(householdId));
        await service.SaveAsync(groceries.Id, Target(2026, 9, 100m, rollover: true));

        var october = await service.GetAsync(2026, 10);

        var line = Line(october, groceries.Id);
        Assert.Equal(60m, line.RolloverIn);
        Assert.Equal(160m, line.Available);
        Assert.Equal(160m, line.Remaining);
        Assert.Equal(0m, line.Spent);
    }

    [Fact]
    public async Task StartFresh_StopsALaterVisitFromCopying()
    {
        await using var dbContext = CreateDbContext();
        await DataSeeder.SeedAsync(dbContext, new FakeTimeProvider(Now));
        var householdId = await CreateHouseholdAsync(dbContext, "user_owner");
        var groceries = await CategoryAsync(dbContext, SystemCategoryKeys.FoodDining);
        var service = CreateService(dbContext, Bind(householdId));
        await service.SaveAsync(groceries.Id, Target(2026, 9, 100m, rollover: true));

        var october = await service.StartFreshAsync(new CategoryTargetMonthRequest
        {
            Year = 2026,
            Month = 10
        });

        Assert.True(october.Saved);
        Assert.Null(october.CopiedFromMonth);
        Assert.Null(Line(october, groceries.Id).Target);
        Assert.Equal(100m, Line(october, groceries.Id).RolloverIn);
        Assert.Equal(1, october.UnassignedRolloverCount);
        var again = await service.GetAsync(2026, 10);
        Assert.Null(Line(again, groceries.Id).Target);
    }

    [Fact]
    public async Task CopyForward_IsIdempotentAndClearKeepsTheMonthStarted()
    {
        await using var dbContext = CreateDbContext();
        await DataSeeder.SeedAsync(dbContext, new FakeTimeProvider(Now));
        var householdId = await CreateHouseholdAsync(dbContext, "user_owner");
        var groceries = await CategoryAsync(dbContext, SystemCategoryKeys.FoodDining);
        var shopping = await CategoryAsync(dbContext, SystemCategoryKeys.Shopping);
        var service = CreateService(dbContext, Bind(householdId));
        await service.SaveAsync(groceries.Id, Target(2026, 9, 100m, rollover: false));
        await service.SaveAsync(shopping.Id, Target(2026, 9, 40m, rollover: false));
        await service.CopyForwardAsync(new CategoryTargetMonthRequest { Year = 2026, Month = 10 });

        var again = await service.CopyForwardAsync(new CategoryTargetMonthRequest
        {
            Year = 2026,
            Month = 10
        });
        var cleared = await service.ClearAsync(groceries.Id, 2026, 10);

        Assert.True(again.Saved);
        Assert.Equal(100m, Line(again, groceries.Id).Target);
        Assert.Null(Line(cleared, groceries.Id).Target);
        Assert.Equal(40m, Line(cleared, shopping.Id).Target);
        var reread = await service.GetAsync(2026, 10);
        Assert.Null(Line(reread, groceries.Id).Target);
        Assert.Equal(40m, Line(await service.GetAsync(2026, 9), shopping.Id).Target);
    }

    [Fact]
    public async Task OtherHousehold_DoesNotSeeOrChangeTargets()
    {
        await using var dbContext = CreateDbContext();
        await DataSeeder.SeedAsync(dbContext, new FakeTimeProvider(Now));
        var firstId = await CreateHouseholdAsync(dbContext, "user_owner_a");
        var secondId = await CreateHouseholdAsync(dbContext, "user_owner_b");
        var groceries = await CategoryAsync(dbContext, SystemCategoryKeys.FoodDining);
        var first = CreateService(dbContext, Bind(firstId));
        await first.SaveAsync(groceries.Id, Target(2026, 10, 80m, rollover: true));

        var second = CreateService(dbContext, Bind(secondId));
        var viewed = await second.GetAsync(2026, 10);

        Assert.Null(Line(viewed, groceries.Id).Target);
        var error = await Assert.ThrowsAsync<BadRequestException>(() =>
            second.ClearAsync(groceries.Id, 2026, 10));
        Assert.Equal("That category has no target.", error.Message);
        Assert.Equal(80m, Line(await first.GetAsync(2026, 10), groceries.Id).Target);
    }

    #region Private Methods

    /// <summary>
    /// The row for one category in a month result.
    /// </summary>
    private static CategoryTargetLineDto Line(CategoryTargetMonthDto month, Guid categoryId)
    {
        return month.Categories.Single(item => item.CategoryId == categoryId);
    }

    /// <summary>
    /// A target save for one month.
    /// </summary>
    private static UpsertCategoryTargetDto Target(int year, int month, decimal amount, bool rollover)
    {
        return new UpsertCategoryTargetDto
        {
            Year = year,
            Month = month,
            Amount = amount,
            Rollover = rollover
        };
    }

    /// <summary>
    /// A system category by its key.
    /// </summary>
    private static async Task<Category> CategoryAsync(CarduiDBContext dbContext, string key)
    {
        return await dbContext.Categories.SingleAsync(category => category.Key == key);
    }

    private static CategoryTargetsService CreateService(CarduiDBContext dbContext, HouseholdScope scope)
    {
        return new CategoryTargetsService(dbContext, new FakeTimeProvider(Now), scope);
    }

    private static HouseholdScope Bind(Guid householdId)
    {
        var scope = new HouseholdScope();
        scope.Bind(householdId, PlanningCurrencyRules.DefaultCode, HouseholdTime.DefaultTimeZoneId);
        return scope;
    }

    private static async Task<Guid> CreateHouseholdAsync(CarduiDBContext dbContext, string ownerId)
    {
        var household = await new HouseholdsService(dbContext, new FakeTimeProvider(Now))
            .GetOrCreateForOwnerAsync(ownerId);
        return household.Id;
    }

    private static async Task<Guid> AddAccountAsync(CarduiDBContext dbContext, Guid householdId)
    {
        var account = new Account
        {
            Id = Guid.NewGuid(),
            HouseholdId = householdId,
            Source = FinancialRecordSource.Manual,
            Provenance = FinancialRecordProvenance.ManualEntry,
            Name = "Checking",
            Type = AccountTypes.Depository,
            IsoCurrencyCode = "USD",
            IsActive = true,
            CurrentBalance = 0m,
            CreatedAt = Now,
            UpdatedAt = Now
        };
        dbContext.Accounts.Add(account);
        await dbContext.SaveChangesAsync();
        return account.Id;
    }

    private static async Task AddTransactionAsync(
        CarduiDBContext dbContext,
        Guid accountId,
        DateOnly date,
        decimal amount,
        Guid? categoryId,
        string name,
        bool pending = false,
        string? currency = "USD",
        FinancialRecordProvenance provenance = FinancialRecordProvenance.ManualEntry)
    {
        dbContext.Transactions.Add(new Transaction
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Source = FinancialRecordSource.Manual,
            Provenance = provenance,
            Date = date,
            Name = name,
            Amount = amount,
            IsoCurrencyCode = currency,
            Pending = pending,
            CategoryId = categoryId,
            CreatedAt = Now,
            UpdatedAt = Now
        });
        await dbContext.SaveChangesAsync();
    }

    private static CarduiDBContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CarduiDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new CarduiDBContext(options);
    }

    #endregion
}
