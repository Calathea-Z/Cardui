using Cardui.Api.Data;
using Cardui.Api.Domain;
using Cardui.Api.Dtos.Household;
using Cardui.Api.Exceptions;
using Cardui.Api.Security;
using Cardui.Api.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Cardui.Tests.Services;

public class FinancialProfileServiceTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 10, 3, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Update_SavesThePlanningCurrencyAndTimeZone()
    {
        await using var dbContext = CreateDbContext();
        var householdId = await CreateHouseholdAsync(dbContext, "user_owner");
        var scope = Bind(householdId);
        var service = CreateService(dbContext, scope);

        var saved = await service.UpdateAsync(new UpdateFinancialProfileDto
        {
            PlanningCurrency = "cad",
            TimeZoneId = "America/Chicago"
        });

        Assert.Equal("CAD", saved.PlanningCurrency);
        Assert.Equal("America/Chicago", saved.TimeZoneId);
        Assert.Equal("CAD", scope.PlanningCurrency);
        Assert.Equal("America/Chicago", scope.TimeZoneId);
    }

    [Theory]
    [InlineData("US")]
    [InlineData("USDD")]
    [InlineData("12A")]
    [InlineData("")]
    public async Task Update_RejectsACurrencyThatIsNotThreeLetters(string currency)
    {
        await using var dbContext = CreateDbContext();
        var householdId = await CreateHouseholdAsync(dbContext, "user_owner");
        var service = CreateService(dbContext, Bind(householdId));

        await Assert.ThrowsAsync<BadRequestException>(() =>
            service.UpdateAsync(new UpdateFinancialProfileDto
            {
                PlanningCurrency = currency,
                TimeZoneId = HouseholdTime.DefaultTimeZoneId
            }));
    }

    [Fact]
    public async Task Update_RejectsAnUnknownTimeZone()
    {
        await using var dbContext = CreateDbContext();
        var householdId = await CreateHouseholdAsync(dbContext, "user_owner");
        var service = CreateService(dbContext, Bind(householdId));

        await Assert.ThrowsAsync<BadRequestException>(() =>
            service.UpdateAsync(new UpdateFinancialProfileDto
            {
                PlanningCurrency = "USD",
                TimeZoneId = "Not/A_Zone"
            }));
    }

    [Fact]
    public async Task Contributors_StayInsideTheHouseholdAndCanBeHidden()
    {
        await using var dbContext = CreateDbContext();
        var firstId = await CreateHouseholdAsync(dbContext, "user_owner_a");
        var secondId = await CreateHouseholdAsync(dbContext, "user_owner_b");
        var firstScope = Bind(firstId);
        var firstService = CreateService(dbContext, firstScope);

        var alex = await firstService.AddContributorAsync(new UpsertHouseholdContributorDto
        {
            Name = "  Alex  ",
            IsVisible = true
        });
        var sam = await firstService.AddContributorAsync(new UpsertHouseholdContributorDto
        {
            Name = "Sam",
            IsVisible = false
        });

        Assert.Equal("Alex", alex.Name);
        Assert.True(alex.IsVisible);
        Assert.False(sam.IsVisible);

        var profile = await firstService.GetAsync();
        Assert.Equal(2, profile.Contributors.Count);
        Assert.Contains(profile.Contributors, x => x.Name == "Sam" && !x.IsVisible);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            firstService.AddContributorAsync(new UpsertHouseholdContributorDto
            {
                Name = "alex",
                IsVisible = true
            }));

        var secondService = CreateService(dbContext, Bind(secondId));
        await Assert.ThrowsAsync<NotFoundException>(() =>
            secondService.UpdateContributorAsync(
                alex.Id,
                new UpsertHouseholdContributorDto
                {
                    Name = "Alex",
                    IsVisible = false
                }));

        var otherProfile = await secondService.GetAsync();
        Assert.Empty(otherProfile.Contributors);

        await firstService.RemoveContributorAsync(sam.Id);
        var remaining = await firstService.GetAsync();
        var only = Assert.Single(remaining.Contributors);
        Assert.Equal(alex.Id, only.Id);
    }

    private static FinancialProfileService CreateService(
        CarduiDBContext dbContext,
        HouseholdScope scope)
    {
        return new FinancialProfileService(
            dbContext,
            new FakeTimeProvider(Now),
            scope);
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

    private static CarduiDBContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CarduiDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new CarduiDBContext(options);
    }
}
