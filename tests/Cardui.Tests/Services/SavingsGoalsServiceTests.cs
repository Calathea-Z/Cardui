using Cardui.Api.Data;
using Cardui.Api.Domain.Accounts;
using Cardui.Api.Dtos.Savings;
using Cardui.Api.Exceptions;
using Cardui.Api.Models;
using Cardui.Api.Security;
using Cardui.Api.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Cardui.Tests.Services;

public class SavingsGoalsServiceTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 6, 15, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Create_StoresTheGoalWithoutChangingTheAccount()
    {
        await using var dbContext = CreateDbContext();
        var householdId = await CreateHouseholdAsync(dbContext, "user_owner");
        var accountId = await AddAccountAsync(dbContext, householdId, "Checking", 400m);
        var service = CreateService(dbContext, householdId);

        var goal = await service.CreateAsync(Draft(SavingsGoalKind.Operating, accountId, 1000m, 0m, true));

        Assert.Equal(SavingsGoalKind.Operating, goal.Kind);
        Assert.Equal("Monthly living spending", goal.Name);
        Assert.Equal("USD", goal.Currency);
        Assert.True(goal.Following);
        Assert.False(goal.ReservedOverridden);
        Assert.Equal(400m, goal.AmountInUse);
        Assert.Equal(400m, await dbContext.Accounts.Where(account => account.Id == accountId).Select(account => account.CurrentBalance).SingleAsync());
        Assert.Empty(dbContext.Transactions);
    }

    [Fact]
    public async Task Create_RejectsASecondOperatingReserveAndACreditAccount()
    {
        await using var dbContext = CreateDbContext();
        var householdId = await CreateHouseholdAsync(dbContext, "user_owner");
        var service = CreateService(dbContext, householdId);
        await service.CreateAsync(Draft(SavingsGoalKind.Operating, null, 500m, 0m, false));
        var cardId = await AddAccountAsync(dbContext, householdId, "Card", 20m, AccountTypes.Credit);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            service.CreateAsync(Draft(SavingsGoalKind.Operating, null, 800m, 0m, false)));
        await Assert.ThrowsAsync<BadRequestException>(() =>
            service.CreateAsync(Draft(SavingsGoalKind.Sinking, cardId, 100m, 0m, true, "Tires")));
    }

    [Fact]
    public async Task Update_KeepsATypedAmountUntilTheBalanceIsUsedAgain()
    {
        await using var dbContext = CreateDbContext();
        var householdId = await CreateHouseholdAsync(dbContext, "user_owner");
        var accountId = await AddAccountAsync(dbContext, householdId, "Savings", 250m);
        var service = CreateService(dbContext, householdId);
        var created = await service.CreateAsync(Draft(SavingsGoalKind.Emergency, accountId, 1000m, 40m, false));

        Assert.True(created.ReservedOverridden);
        Assert.Equal(40m, created.AmountInUse);

        var followed = await service.UpdateAsync(
            created.Id,
            Draft(SavingsGoalKind.Emergency, accountId, 1000m, 40m, true));

        Assert.False(followed.ReservedOverridden);
        Assert.Equal(250m, followed.AmountInUse);
        Assert.Equal(250m, await dbContext.Accounts.Where(account => account.Id == accountId).Select(account => account.CurrentBalance).SingleAsync());
    }

    [Fact]
    public async Task Update_ClearingTheAccountKeepsTheAmountAndRejectsASecondFollow()
    {
        await using var dbContext = CreateDbContext();
        var householdId = await CreateHouseholdAsync(dbContext, "user_owner");
        var savingsId = await AddAccountAsync(dbContext, householdId, "Savings", 250m);
        var otherId = await AddAccountAsync(dbContext, householdId, "Other", 80m);
        var service = CreateService(dbContext, householdId);
        var emergency = await service.CreateAsync(Draft(SavingsGoalKind.Emergency, savingsId, 1000m, 0m, true));
        var trip = await service.CreateAsync(Draft(SavingsGoalKind.Sinking, null, 300m, 20m, false, "Trip"));

        await Assert.ThrowsAsync<BadRequestException>(() =>
            service.UpdateAsync(trip.Id, Draft(SavingsGoalKind.Sinking, savingsId, 300m, 20m, true, "Trip")));

        var cleared = await service.UpdateAsync(
            emergency.Id,
            Draft(SavingsGoalKind.Emergency, null, 1000m, emergency.AmountInUse, false));
        Assert.False(cleared.Following);
        Assert.Null(cleared.AccountId);
        Assert.Equal(250m, cleared.AmountInUse);

        var moved = await service.UpdateAsync(
            trip.Id,
            Draft(SavingsGoalKind.Sinking, otherId, 300m, 20m, false, "Trip"));
        Assert.Equal(otherId, moved.AccountId);
        Assert.True(moved.ReservedOverridden);
        Assert.Equal(20m, moved.AmountInUse);
    }

    [Fact]
    public async Task GetGoals_LeavesAnotherHouseholdOut()
    {
        await using var dbContext = CreateDbContext();
        var ownerId = await CreateHouseholdAsync(dbContext, "user_owner");
        var otherId = await CreateHouseholdAsync(dbContext, "user_other");
        await CreateService(dbContext, ownerId).CreateAsync(Draft(SavingsGoalKind.Operating, null, 100m, 0m, false));
        await CreateService(dbContext, otherId).CreateAsync(
            Draft(SavingsGoalKind.Sinking, null, 50m, 0m, false, "Trip"));

        var goals = await CreateService(dbContext, ownerId).GetGoalsAsync();

        var goal = Assert.Single(goals);
        Assert.Equal(SavingsGoalKind.Operating, goal.Kind);
    }

    [Fact]
    public async Task Delete_RemovesTheGoalAndLeavesTheAccount()
    {
        await using var dbContext = CreateDbContext();
        var householdId = await CreateHouseholdAsync(dbContext, "user_owner");
        var accountId = await AddAccountAsync(dbContext, householdId, "Savings", 90m);
        var service = CreateService(dbContext, householdId);
        var goal = await service.CreateAsync(Draft(SavingsGoalKind.Sinking, accountId, 200m, 0m, true, "Tires"));

        await service.DeleteAsync(goal.Id);

        Assert.Empty(await service.GetGoalsAsync());
        Assert.Equal(90m, await dbContext.Accounts.Where(account => account.Id == accountId).Select(account => account.CurrentBalance).SingleAsync());
    }

    private static UpsertSavingsGoalDto Draft(
        SavingsGoalKind kind,
        Guid? accountId,
        decimal target,
        decimal reserved,
        bool useAccountBalance,
        string? name = null)
    {
        return new UpsertSavingsGoalDto
        {
            Kind = kind,
            Name = name,
            TargetAmount = kind is SavingsGoalKind.Operating or SavingsGoalKind.Floor ? null : target,
            TargetDate = kind is SavingsGoalKind.Operating or SavingsGoalKind.Floor ? null : new DateOnly(2026, 12, 15),
            MonthlyAmount = kind == SavingsGoalKind.Operating ? target : null,
            ReadyDay = kind == SavingsGoalKind.Operating ? 1 : null,
            FloorAmount = kind == SavingsGoalKind.Floor ? target : null,
            ReservedAmount = reserved,
            AccountId = accountId,
            UseAccountBalance = useAccountBalance
        };
    }

    private static SavingsGoalsService CreateService(CarduiDBContext dbContext, Guid householdId)
    {
        var scope = new HouseholdScope();
        scope.Bind(householdId);
        return new SavingsGoalsService(dbContext, new FakeTimeProvider(Now), scope);
    }

    private static async Task<Guid> CreateHouseholdAsync(CarduiDBContext dbContext, string ownerId)
    {
        var household = await new HouseholdsService(dbContext, new FakeTimeProvider(Now))
            .GetOrCreateForOwnerAsync(ownerId);
        return household.Id;
    }

    private static async Task<Guid> AddAccountAsync(
        CarduiDBContext dbContext,
        Guid householdId,
        string name,
        decimal balance,
        string? type = null)
    {
        var account = new Account
        {
            Id = Guid.NewGuid(),
            HouseholdId = householdId,
            Source = FinancialRecordSource.Manual,
            Provenance = FinancialRecordProvenance.ManualEntry,
            Name = name,
            Type = type ?? AccountTypes.Depository,
            CurrentBalance = balance,
            IsActive = true,
            CreatedAt = Now,
            UpdatedAt = Now
        };
        dbContext.Accounts.Add(account);
        await dbContext.SaveChangesAsync();
        return account.Id;
    }

    private static CarduiDBContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CarduiDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new CarduiDBContext(options);
    }
}
