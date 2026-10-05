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
        Assert.Null(saved.GrossPayAmount);
        Assert.Equal(2400.50m, saved.TakeHomeAmount);
        Assert.NotEqual(saved.AverageMonthlyAmount, saved.TakeHomeAmount);
        Assert.Equal(new DateOnly(2026, 10, 16), saved.UpcomingPaymentDates[0]);
    }

    [Fact]
    public async Task Create_StoresOptionalGrossPayAndRejectsGrossBelowNet()
    {
        await using var dbContext = CreateDbContext();
        var householdId = await CreateHouseholdAsync(dbContext, "user_owner");
        var service = CreateService(dbContext, Bind(householdId));
        var paycheck = Paycheck();
        paycheck.GrossPayAmount = 3100m;

        var saved = await service.CreateAsync(paycheck);

        Assert.Equal(3100m, saved.GrossPayAmount);
        Assert.Equal(2400.50m, saved.TakeHomeAmount);

        var lowGross = Paycheck();
        lowGross.Name = "Side work";
        lowGross.GrossPayAmount = 2000m;
        var error = await Assert.ThrowsAsync<BadRequestException>(() => service.CreateAsync(lowGross));
        Assert.Equal("Gross pay cannot be lower than the typical net pay.", error.Message);
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

    [Fact]
    public async Task Create_StoresScenariosAndRaisesWithoutReplacingTypical()
    {
        await using var dbContext = CreateDbContext();
        var householdId = await CreateHouseholdAsync(dbContext, "user_owner");
        var service = CreateService(dbContext, Bind(householdId));

        var saved = await service.CreateAsync(PaycheckWithRange());

        Assert.Equal(2400.50m, saved.TakeHomeAmount);
        Assert.Equal(1800m, saved.LowTakeHomeAmount);
        Assert.Equal(3000m, saved.StrongTakeHomeAmount);
        Assert.Equal(2, saved.Raises.Count);
        Assert.Equal(new DateOnly(2026, 10, 16), saved.Raises[0].EffectiveDate);
        Assert.Equal(2500m, saved.Raises[0].TakeHomeAmount);
        Assert.Equal(new DateOnly(2027, 1, 1), saved.Raises[1].EffectiveDate);
        Assert.Equal(2600m, saved.Raises[1].TakeHomeAmount);
        Assert.NotEqual(Guid.Empty, saved.Raises[0].Id);
    }

    [Fact]
    public async Task Update_AddsARaiseToASourceThatHadNone()
    {
        await using var dbContext = CreateDbContext();
        var householdId = await CreateHouseholdAsync(dbContext, "user_owner");
        var service = CreateService(dbContext, Bind(householdId));
        var saved = await service.CreateAsync(Paycheck());

        var updated = Paycheck();
        updated.NextPaymentDate = new DateOnly(2026, 10, 5);
        updated.Raises =
        [
            new UpsertIncomeRaiseDto
            {
                EffectiveDate = new DateOnly(2026, 10, 5),
                TakeHomeAmount = 2500m
            }
        ];

        var result = await service.UpdateAsync(saved.Id, updated);

        var raise = Assert.Single(result.Raises);
        Assert.Equal(new DateOnly(2026, 10, 5), raise.EffectiveDate);
        Assert.Equal(2500m, raise.TakeHomeAmount);
        Assert.Equal(2400.50m, result.TakeHomeAmount);
    }

    [Fact]
    public async Task Update_ReplacesRaisesAndClearsStrong()
    {
        await using var dbContext = CreateDbContext();
        var householdId = await CreateHouseholdAsync(dbContext, "user_owner");
        var service = CreateService(dbContext, Bind(householdId));
        var saved = await service.CreateAsync(PaycheckWithRange());

        var updated = Paycheck();
        updated.LowTakeHomeAmount = 1900m;
        updated.Raises =
        [
            new UpsertIncomeRaiseDto
            {
                EffectiveDate = new DateOnly(2026, 10, 16),
                TakeHomeAmount = 2550m
            }
        ];
        var result = await service.UpdateAsync(saved.Id, updated);

        Assert.Equal(2400.50m, result.TakeHomeAmount);
        Assert.Equal(1900m, result.LowTakeHomeAmount);
        Assert.Null(result.StrongTakeHomeAmount);
        Assert.Equal("USD", result.Currency);
        var raise = Assert.Single(result.Raises);
        Assert.Equal(saved.Raises[0].Id, raise.Id);
        Assert.Equal(2550m, raise.TakeHomeAmount);
        Assert.Equal(1, await dbContext.IncomeRaises.CountAsync());
    }

    [Fact]
    public async Task Create_RejectsAScenarioOrRaiseThatBreaksThePaymentRules()
    {
        await using var dbContext = CreateDbContext();
        var householdId = await CreateHouseholdAsync(dbContext, "user_owner");
        var service = CreateService(dbContext, Bind(householdId));

        var low = Paycheck();
        low.LowTakeHomeAmount = 3000m;
        var lowError = await Assert.ThrowsAsync<BadRequestException>(() => service.CreateAsync(low));
        Assert.Equal("Low net pay cannot be higher than the typical amount.", lowError.Message);

        var early = Paycheck();
        early.Name = "Side work";
        early.Raises =
        [
            new UpsertIncomeRaiseDto
            {
                EffectiveDate = new DateOnly(2026, 10, 1),
                TakeHomeAmount = 2600m
            }
        ];
        var earlyError = await Assert.ThrowsAsync<BadRequestException>(() => service.CreateAsync(early));
        Assert.Equal("Enter a raise date on or after the next payment.", earlyError.Message);
        Assert.Empty(await service.GetIncomeSourcesAsync());
    }

    [Fact]
    public async Task Delete_RemovesTheRaisesAndFreesTheName()
    {
        await using var dbContext = CreateDbContext();
        var householdId = await CreateHouseholdAsync(dbContext, "user_owner");
        var service = CreateService(dbContext, Bind(householdId));
        var saved = await service.CreateAsync(PaycheckWithRange());

        await service.DeleteAsync(saved.Id);

        Assert.Empty(await service.GetIncomeSourcesAsync());
        Assert.Empty(dbContext.IncomeRaises);
        var again = await service.CreateAsync(Paycheck());
        Assert.Equal("Paycheck", again.Name);
        Assert.Empty(again.Raises);
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

    private static UpsertIncomeSourceDto PaycheckWithRange()
    {
        var dto = Paycheck();
        dto.LowTakeHomeAmount = 1800m;
        dto.StrongTakeHomeAmount = 3000m;
        dto.Raises =
        [
            new UpsertIncomeRaiseDto
            {
                EffectiveDate = new DateOnly(2027, 1, 1),
                TakeHomeAmount = 2600m
            },
            new UpsertIncomeRaiseDto
            {
                EffectiveDate = new DateOnly(2026, 10, 16),
                TakeHomeAmount = 2500m
            }
        ];
        return dto;
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
