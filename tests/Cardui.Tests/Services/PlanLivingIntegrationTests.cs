using Cardui.Api.Data;
using Cardui.Api.Dtos.Household;
using Cardui.Api.Dtos.Income;
using Cardui.Api.Dtos.Living;
using Cardui.Api.Dtos.Savings;
using Cardui.Api.Models;
using Cardui.Api.Security;
using Cardui.Api.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Cardui.Tests.Services;

public class PlanLivingIntegrationTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 10, 8, 15, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Plan_UsesSharedPayAndMonthlyLivingSpendingOnce()
    {
        await using var dbContext = CreateDbContext();
        var time = new FakeTimeProvider(Now);
        var household = await new HouseholdsService(dbContext, time)
            .GetOrCreateForOwnerAsync("plan_living_owner");
        var scope = new HouseholdScope();
        scope.Bind(household.Id);
        var contributor = await new FinancialProfileService(dbContext, time, scope)
            .AddContributorAsync(new UpsertHouseholdContributorDto { Name = "Alex", IsVisible = true });
        var incomes = new IncomeSourcesService(dbContext, time, scope);
        await incomes.CreateAsync(new UpsertIncomeSourceDto
        {
            Name = "Paycheck",
            TakeHomeAmount = 4000m,
            Cadence = IncomeCadence.Monthly,
            NextPaymentDate = new DateOnly(2026, 10, 16),
            ContributorId = contributor.Id,
            Reliability = IncomeReliability.Steady
        });
        var savings = new SavingsGoalsService(dbContext, time, scope);
        await savings.CreateAsync(new UpsertSavingsGoalDto
        {
            Kind = SavingsGoalKind.Operating,
            MonthlyAmount = 300m,
            ReadyDay = 8,
            ReservedAmount = 0m
        });
        var living = CreateLivingService(dbContext, time, scope, incomes, savings);
        await living.SetContributionAsync(
            contributor.Id,
            new UpsertLivingContributionDto { MonthlyAmount = 2000m });
        var plan = new PlanService(
            new DebtsService(dbContext, time, scope),
            incomes,
            new ObligationsService(dbContext, time, scope),
            new AccountsService(dbContext, time, scope),
            savings,
            living,
            time,
            scope);

        var report = await plan.GetRecoveryAsync(0m);
        var days = report.CashOutlook.Rollover.Typical.Days;

        Assert.Equal(300m, report.LivingSpendingMonthly);
        Assert.Equal(300m, days.Single(day => day.Date == new DateOnly(2026, 10, 8)).LivingSpending);
        Assert.Equal(2000m, days.Single(day => day.Date == new DateOnly(2026, 10, 16)).Income);
        Assert.Equal(-300m, days[0].Cash);
    }

    #region Private Methods

    /// <summary>
    /// Creates Living with the same scoped services Plan reads.
    /// </summary>
    private static LivingService CreateLivingService(
        CarduiDBContext dbContext,
        TimeProvider time,
        HouseholdScope scope,
        IncomeSourcesService incomes,
        SavingsGoalsService savings)
    {
        return new LivingService(
            dbContext,
            incomes,
            new ObligationsService(dbContext, time, scope),
            new DebtsService(dbContext, time, scope),
            savings,
            time,
            scope);
    }

    /// <summary>
    /// Creates an isolated in-memory database for one integration test.
    /// </summary>
    private static CarduiDBContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CarduiDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new CarduiDBContext(options);
    }

    #endregion
}
