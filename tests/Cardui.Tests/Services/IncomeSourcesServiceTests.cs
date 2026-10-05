using Cardui.Api.Data;
using Cardui.Api.Dtos.Household;
using Cardui.Api.Dtos.Income;
using Cardui.Api.Exceptions;
using Cardui.Api.Models;
using Cardui.Api.Security;
using Cardui.Api.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Cardui.Tests.Services;

public class IncomeSourcesServiceTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 10, 4, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Create_StoresOnePaymentInThePlanningCurrency()
    {
        await using var dbContext = CreateDbContext();
        var householdId = await CreateHouseholdAsync(dbContext, "user_owner");
        await SetPlanningCurrencyAsync(dbContext, householdId, "CAD");
        var profile = CreateProfileService(dbContext, Bind(householdId));
        var alex = await profile.AddContributorAsync(new UpsertHouseholdContributorDto
        {
            Name = "Alex",
            IsVisible = true
        });
        var service = CreateService(dbContext, Bind(householdId));

        var saved = await service.CreateAsync(Paycheck(alex.Id));

        Assert.Equal("Paycheck", saved.Name);
        Assert.Equal(2400.50m, saved.TakeHomeAmount);
        Assert.Equal("CAD", saved.Currency);
        Assert.Equal(IncomeCadence.Biweekly, saved.Cadence);
        Assert.Equal(new DateOnly(2026, 10, 16), saved.NextPaymentDate);
        Assert.Equal(alex.Id, saved.ContributorId);
        Assert.Equal("Alex", saved.ContributorName);
        Assert.Equal(IncomeReliability.Steady, saved.Reliability);
    }

    [Fact]
    public async Task Create_RejectsADuplicateNameAndAForeignContributor()
    {
        await using var dbContext = CreateDbContext();
        var firstId = await CreateHouseholdAsync(dbContext, "user_owner_a");
        var secondId = await CreateHouseholdAsync(dbContext, "user_owner_b");
        var first = CreateService(dbContext, Bind(firstId));
        var secondProfile = CreateProfileService(dbContext, Bind(secondId));
        var other = await secondProfile.AddContributorAsync(new UpsertHouseholdContributorDto
        {
            Name = "Sam",
            IsVisible = true
        });

        await first.CreateAsync(Paycheck());

        var duplicate = Paycheck();
        duplicate.Name = "paycheck";
        await Assert.ThrowsAsync<BadRequestException>(() => first.CreateAsync(duplicate));

        var foreign = Paycheck(other.Id);
        foreign.Name = "Other job";
        await Assert.ThrowsAsync<BadRequestException>(() => first.CreateAsync(foreign));
    }

    [Fact]
    public async Task Sources_StayInsideTheHousehold()
    {
        await using var dbContext = CreateDbContext();
        var firstId = await CreateHouseholdAsync(dbContext, "user_owner_a");
        var secondId = await CreateHouseholdAsync(dbContext, "user_owner_b");
        var first = CreateService(dbContext, Bind(firstId));
        var saved = await first.CreateAsync(Paycheck());

        var second = CreateService(dbContext, Bind(secondId));
        Assert.Empty(await second.GetIncomeSourcesAsync());
        await Assert.ThrowsAsync<NotFoundException>(() =>
            second.UpdateAsync(saved.Id, Paycheck()));
    }

    [Fact]
    public async Task Delete_RemovesTheSourceAndFreesTheName()
    {
        await using var dbContext = CreateDbContext();
        var householdId = await CreateHouseholdAsync(dbContext, "user_owner");
        var otherId = await CreateHouseholdAsync(dbContext, "user_owner_b");
        var service = CreateService(dbContext, Bind(householdId));
        var saved = await service.CreateAsync(Paycheck());

        await service.DeleteAsync(saved.Id);
        Assert.Empty(await service.GetIncomeSourcesAsync());

        var again = await service.CreateAsync(Paycheck());
        Assert.Equal("Paycheck", again.Name);

        var other = CreateService(dbContext, Bind(otherId));
        await Assert.ThrowsAsync<NotFoundException>(() => other.DeleteAsync(again.Id));
        Assert.Single(await service.GetIncomeSourcesAsync());
    }

    [Fact]
    public async Task Update_KeepsTheCurrencyFromWhenTheSourceWasCreated()
    {
        await using var dbContext = CreateDbContext();
        var householdId = await CreateHouseholdAsync(dbContext, "user_owner");
        var service = CreateService(dbContext, Bind(householdId));
        var saved = await service.CreateAsync(Paycheck());
        await SetPlanningCurrencyAsync(dbContext, householdId, "EUR");

        var updated = Paycheck();
        updated.Name = "Paycheck";
        updated.TakeHomeAmount = 2500m;
        updated.Reliability = IncomeReliability.Variable;
        var result = await service.UpdateAsync(saved.Id, updated);

        Assert.Equal(2500m, result.TakeHomeAmount);
        Assert.Equal(IncomeReliability.Variable, result.Reliability);
        Assert.Equal("USD", result.Currency);
    }

    [Fact]
    public async Task RemoveContributor_LeavesTheIncomeSourceWithoutThatPerson()
    {
        await using var dbContext = CreateDbContext();
        var householdId = await CreateHouseholdAsync(dbContext, "user_owner");
        var scope = Bind(householdId);
        var profile = CreateProfileService(dbContext, scope);
        var alex = await profile.AddContributorAsync(new UpsertHouseholdContributorDto
        {
            Name = "Alex",
            IsVisible = false
        });
        var service = CreateService(dbContext, scope);
        var saved = await service.CreateAsync(Paycheck(alex.Id));

        await profile.RemoveContributorAsync(alex.Id);

        var listed = Assert.Single(await service.GetIncomeSourcesAsync());
        Assert.Equal(saved.Id, listed.Id);
        Assert.Null(listed.ContributorId);
        Assert.Null(listed.ContributorName);
        Assert.Equal(2400.50m, listed.TakeHomeAmount);
    }

    private static UpsertIncomeSourceDto Paycheck(Guid? contributorId = null)
    {
        return new UpsertIncomeSourceDto
        {
            Name = "  Paycheck  ",
            TakeHomeAmount = 2400.50m,
            Cadence = IncomeCadence.Biweekly,
            NextPaymentDate = new DateOnly(2026, 10, 16),
            ContributorId = contributorId,
            Reliability = IncomeReliability.Steady
        };
    }

    private static IncomeSourcesService CreateService(
        CarduiDBContext dbContext,
        HouseholdScope scope)
    {
        return new IncomeSourcesService(dbContext, new FakeTimeProvider(Now), scope);
    }

    private static FinancialProfileService CreateProfileService(
        CarduiDBContext dbContext,
        HouseholdScope scope)
    {
        return new FinancialProfileService(dbContext, new FakeTimeProvider(Now), scope);
    }

    private static HouseholdScope Bind(Guid householdId)
    {
        var scope = new HouseholdScope();
        scope.Bind(householdId);
        return scope;
    }

    private static async Task<Guid> CreateHouseholdAsync(
        CarduiDBContext dbContext,
        string ownerId)
    {
        var household = await new HouseholdsService(dbContext, new FakeTimeProvider(Now))
            .GetOrCreateForOwnerAsync(ownerId);
        return household.Id;
    }

    private static async Task SetPlanningCurrencyAsync(
        CarduiDBContext dbContext,
        Guid householdId,
        string currency)
    {
        var household = await dbContext.Households.SingleAsync(x => x.Id == householdId);
        household.PlanningCurrency = currency;
        await dbContext.SaveChangesAsync();
    }

    private static CarduiDBContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CarduiDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new CarduiDBContext(options);
    }
}
