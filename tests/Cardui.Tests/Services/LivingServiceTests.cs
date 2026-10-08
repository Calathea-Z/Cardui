using Cardui.Api.Domain.Living;
using Cardui.Api.Data;
using Cardui.Api.Dtos.Household;
using Cardui.Api.Dtos.Income;
using Cardui.Api.Dtos.Living;
using Cardui.Api.Dtos.Savings;
using Cardui.Api.Exceptions;
using Cardui.Api.Models;
using Cardui.Api.Security;
using Cardui.Api.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Cardui.Tests.Services;

public class LivingServiceTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 10, 8, 15, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task SetContribution_ScalesPayAndDoesNotChangeThePaycheck()
    {
        await using var dbContext = CreateDbContext();
        var householdId = await CreateHouseholdAsync(dbContext, "user_owner");
        var scope = Bind(householdId);
        var contributor = await new FinancialProfileService(dbContext, new FakeTimeProvider(Now), scope)
            .AddContributorAsync(new UpsertHouseholdContributorDto { Name = "Alex", IsVisible = true });
        await new IncomeSourcesService(dbContext, new FakeTimeProvider(Now), scope)
            .CreateAsync(new UpsertIncomeSourceDto
            {
                Name = "Paycheck",
                TakeHomeAmount = 4000m,
                Cadence = IncomeCadence.Monthly,
                NextPaymentDate = new DateOnly(2026, 10, 16),
                ContributorId = contributor.Id,
                Reliability = IncomeReliability.Steady
            });
        var service = CreateService(dbContext, scope);

        var page = await service.SetContributionAsync(
            contributor.Id,
            new UpsertLivingContributionDto { MonthlyAmount = 2500m });

        var person = Assert.Single(page.Contributions);
        Assert.Equal(2500m, person.MonthlyAmount);
        Assert.Equal(4000m, person.RecordedMonthly);
        Assert.Equal(2500m, person.SharedMonthly);
        Assert.Equal(1500m, person.KeptMonthly);
        Assert.Equal(4000m, await dbContext.IncomeSources.Select(source => source.TakeHomeAmount).SingleAsync());
        Assert.Empty(dbContext.Transactions);
    }

    [Fact]
    public async Task Get_UsesTheOneMonthlyLivingSpendingAmountInTheGap()
    {
        await using var dbContext = CreateDbContext();
        var householdId = await CreateHouseholdAsync(dbContext, "user_owner");
        var scope = Bind(householdId);
        var time = new FakeTimeProvider(Now);
        await new SavingsGoalsService(dbContext, time, scope).CreateAsync(new UpsertSavingsGoalDto
        {
            Kind = SavingsGoalKind.Operating,
            MonthlyAmount = 300m,
            ReadyDay = 1,
            ReservedAmount = 0m
        });

        var page = await CreateService(dbContext, scope).GetAsync();

        Assert.NotNull(page.LivingSpending);
        Assert.Equal(300m, page.Gap.LivingSpendingMonthly);
        Assert.Empty(dbContext.Transactions);
    }

    [Fact]
    public async Task SetContribution_RejectsANegativeAmountAndStoresZero()
    {
        await using var dbContext = CreateDbContext();
        var householdId = await CreateHouseholdAsync(dbContext, "user_owner");
        var scope = Bind(householdId);
        var contributor = await new FinancialProfileService(dbContext, new FakeTimeProvider(Now), scope)
            .AddContributorAsync(new UpsertHouseholdContributorDto { Name = "Alex", IsVisible = true });
        var service = CreateService(dbContext, scope);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            service.SetContributionAsync(contributor.Id, new UpsertLivingContributionDto { MonthlyAmount = -1m }));
        var page = await service.SetContributionAsync(
            contributor.Id,
            new UpsertLivingContributionDto { MonthlyAmount = 0m });

        Assert.Equal(0m, page.Contributions[0].MonthlyAmount);
        Assert.Equal(ContributionLimit.Unplaced, page.Contributions[0].Limit);
    }

    private static LivingService CreateService(CarduiDBContext dbContext, HouseholdScope scope)
    {
        var time = new FakeTimeProvider(Now);
        return new LivingService(
            dbContext,
            new IncomeSourcesService(dbContext, time, scope),
            new ObligationsService(dbContext, time, scope),
            new DebtsService(dbContext, time, scope),
            new SavingsGoalsService(dbContext, time, scope),
            time,
            scope);
    }

    private static HouseholdScope Bind(Guid householdId)
    {
        var scope = new HouseholdScope();
        scope.Bind(householdId);
        return scope;
    }

    private static async Task<Guid> CreateHouseholdAsync(CarduiDBContext dbContext, string ownerId)
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
