using Cardui.Api.Data;
using Cardui.Api.Domain.Accounts;
using Cardui.Api.Domain.Planning;
using Cardui.Api.Domain.Recovery;
using Cardui.Api.Dtos.Account;
using Cardui.Api.Dtos.Debts;
using Cardui.Api.Dtos.Plan;
using Cardui.Api.Dtos.Planning;
using Cardui.Api.Dtos.Savings;
using Cardui.Api.Exceptions;
using Cardui.Api.Models;
using Cardui.Api.Security;
using Cardui.Api.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Cardui.Tests.Services;

public class PlanningForecastsTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 10, 16, 18, 0, 0, TimeSpan.Zero);

    private static readonly DateOnly Today = new(2026, 10, 16);

    [Fact]
    public async Task Describe_ReturnsAnEmptyForecastAndWritesNothing()
    {
        await using var db = CreateDbContext();
        var household = await SeedAsync(db, withDebt: false);

        var forecast = await household.Forecasts.DescribeAsync(new PlanScenarioQuery());

        Assert.Equal(PlanningForecastRules.CalculationVersion, forecast.CalculationVersion);
        Assert.Equal("USD", forecast.PlanningCurrency);
        Assert.Contains("No debt is recorded.", forecast.Statement, StringComparison.Ordinal);
        Assert.Equal(1, CountOf(forecast.Statement, "No monthly flexible spending is recorded."));
        Assert.Equal(1, CountOf(forecast.Statement, "No cash floor is saved."));
        Assert.Contains("Interest", forecast.Assumptions.Select(assumption => assumption.Term));
        Assert.Null(PlanningForecastRules.Reject(forecast.Statement, AsForecast(forecast)));
        Assert.Equal(0, await db.Debts.CountAsync());
        Assert.Equal(0, await db.PlanVersions.CountAsync());
        Assert.Equal(0, await db.Conversations.CountAsync());
    }

    [Fact]
    public async Task Describe_CopiesThePlanCalculation()
    {
        await using var db = CreateDbContext();
        var household = await SeedAsync(db, withDebt: true);
        var scenario = new PlanScenarioQuery { MonthlyExtra = 25m, LivingSpendingMonthly = 125m };

        var recovery = await household.Plan.GetRecoveryAsync(scenario);
        var forecast = await household.Forecasts.DescribeAsync(scenario);
        var day = recovery.CashOutlook.Rollover.Typical.Days[0];
        var interest = Figure(
            forecast,
            PlanningForecastFigureKind.TotalInterest,
            PayoffRolloverKind.Rollover);

        Assert.Equal(recovery.Rollover.TotalInterest, interest.Amount);
        Assert.Equal(
            day.Cash,
            Figure(forecast, PlanningForecastFigureKind.DayCash, PayoffRolloverKind.Rollover, day.Date).Amount);
        Assert.Equal(25m, Figure(forecast, PlanningForecastFigureKind.MonthlyExtra).Amount);
        Assert.Equal(125m, Figure(forecast, PlanningForecastFigureKind.FlexibleSpending).Amount);
        Assert.Contains("125.00 USD", forecast.Statement, StringComparison.Ordinal);
        Assert.Contains("flexible spending", Detail(forecast, "Spending"), StringComparison.Ordinal);
        Assert.Contains(forecast.Tradeoffs, tradeoff => tradeoff.Kind == PlanningForecastTradeoffKind.FreedCash);
        Assert.Contains(forecast.Tradeoffs, tradeoff => tradeoff.Kind == PlanningForecastTradeoffKind.PayoffPriority);
        Assert.Null(PlanningForecastRules.Reject(forecast.Statement, AsForecast(forecast)));
        Assert.Equal(
            "A forecast cannot guarantee a result.",
            PlanningForecastRules.Reject("You will be debt-free with 9999.99 USD.", AsForecast(forecast)));
        Assert.DoesNotContain(forecast.Figures, figure => figure.Name == null && figure.Amount == 9999.99m);
        Assert.Equal(1, await db.Debts.CountAsync());
        Assert.Equal(0, await db.PlanVersions.CountAsync());
        Assert.Equal(0, await db.Conversations.CountAsync());
    }

    [Fact]
    public async Task Describe_StaysInsideTheHouseholdAndRejectsAMissingOne()
    {
        await using var db = CreateDbContext();
        var household = await SeedAsync(db, withDebt: true);
        var other = await new HouseholdsService(db, household.Time).GetOrCreateForOwnerAsync("owner_other");

        var home = await household.Forecasts.DescribeAsync(new PlanScenarioQuery());
        household.Scope.Bind(other.Id);
        var foreign = await household.Forecasts.DescribeAsync(new PlanScenarioQuery());
        var missing = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateForecasts(db, new HouseholdScope(), household.Time).DescribeAsync(new PlanScenarioQuery()));
        var months = await Assert.ThrowsAsync<BadRequestException>(() =>
            CreateForecasts(db, household.Scope, household.Time).DescribeAsync(
                new PlanScenarioQuery { IncomeLossMonths = 99 }));

        Assert.Contains(home.Figures, figure => figure.Name == "Card");
        Assert.DoesNotContain(foreign.Figures, figure => figure.Name == "Card");
        Assert.DoesNotContain("Card", foreign.Statement, StringComparison.Ordinal);
        Assert.Equal("A household scope is required.", missing.Message);
        Assert.Equal("Income loss has to be from 0 to 18 months.", months.Message);
        Assert.Equal(1, await db.Debts.CountAsync());
        Assert.Equal(0, await db.PlanVersions.CountAsync());
    }

    #region Private Methods

    private static PlanningForecast AsForecast(PlanningForecastDto dto)
    {
        return new PlanningForecast(
            dto.CalculationVersion,
            dto.PlanningCurrency,
            dto.Figures.Select(figure => new PlanningForecastFigure(
                figure.Kind,
                figure.Path,
                figure.Priority,
                figure.Basis,
                figure.Amount,
                figure.On,
                figure.Count,
                figure.HorizonMonths,
                figure.Name)).ToList(),
            dto.Assumptions
                .Select(assumption => new PlanningForecastAssumption(assumption.Term, assumption.Detail))
                .ToList(),
            dto.Tradeoffs
                .Select(tradeoff => new PlanningForecastTradeoff(
                    tradeoff.Kind,
                    tradeoff.Path,
                    tradeoff.Priority,
                    tradeoff.Basis,
                    tradeoff.Explanation))
                .ToList(),
            dto.Names,
            dto.Statement);
    }

    private static PlanningForecastFigureDto Figure(
        PlanningForecastDto forecast,
        PlanningForecastFigureKind kind,
        PayoffRolloverKind? path = null,
        DateOnly? on = null)
    {
        return Assert.Single(forecast.Figures, figure =>
            figure.Kind == kind
            && figure.Path == path
            && (on is null || figure.On == on));
    }

    private static string Detail(PlanningForecastDto forecast, string term)
    {
        return Assert.Single(forecast.Assumptions, assumption => assumption.Term == term).Detail;
    }

    /// <summary>
    /// How many times a sentence appears, so a gap is said once.
    /// </summary>
    private static int CountOf(string text, string sentence)
    {
        var count = 0;
        var index = 0;
        while (index < text.Length)
        {
            var found = text.IndexOf(sentence, index, StringComparison.Ordinal);
            if (found < 0)
            {
                return count;
            }

            count++;
            index = found + sentence.Length;
        }

        return count;
    }

    private static async Task<HouseholdHarness> SeedAsync(CarduiDBContext db, bool withDebt)
    {
        var time = new FakeTimeProvider(Now);
        var home = await new HouseholdsService(db, time).GetOrCreateForOwnerAsync("owner_home");
        var scope = new HouseholdScope();
        scope.Bind(home.Id);
        var (forecasts, plan) = Create(db, scope, time);
        if (withDebt)
        {
            await new AccountsService(db, time, scope).CreateManualAccountAsync(new CreateManualAccountDto
            {
                Name = "Checking",
                Type = AccountTypes.Depository,
                IsoCurrencyCode = "USD",
                OpeningBalance = 500m,
                OpeningBalanceDate = Today
            });
            await new DebtsService(db, time, scope).CreateAsync(new UpsertDebtDto
            {
                Name = "Card",
                Kind = DebtKind.Revolving,
                Balance = 200m,
                BalanceAsOf = Today,
                Apr = 12m,
                MinimumPayment = 50m,
                NextDueDate = new DateOnly(2026, 10, 20)
            });
            await new SavingsGoalsService(db, time, scope).CreateAsync(new UpsertSavingsGoalDto
            {
                Kind = SavingsGoalKind.Operating,
                MonthlyAmount = 300m,
                ReadyDay = 16,
                ReservedAmount = 0m
            });
        }

        return new HouseholdHarness(forecasts, plan, time, scope);
    }

    private static PlanningForecasts CreateForecasts(
        CarduiDBContext db,
        HouseholdScope scope,
        TimeProvider time)
    {
        return Create(db, scope, time).Forecasts;
    }

    private static (PlanningForecasts Forecasts, PlanService Plan) Create(
        CarduiDBContext db,
        HouseholdScope scope,
        TimeProvider time)
    {
        var accounts = new AccountsService(db, time, scope);
        var incomes = new IncomeSourcesService(db, time, scope);
        var obligations = new ObligationsService(db, time, scope);
        var debts = new DebtsService(db, time, scope);
        var savings = new SavingsGoalsService(db, time, scope);
        var living = new LivingService(db, incomes, obligations, debts, savings, time, scope);
        var plan = new PlanService(debts, incomes, obligations, accounts, savings, living, time, scope);
        return (new PlanningForecasts(plan, scope), plan);
    }

    private static CarduiDBContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CarduiDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new CarduiDBContext(options);
    }

    private sealed record HouseholdHarness(
        PlanningForecasts Forecasts,
        PlanService Plan,
        FakeTimeProvider Time,
        HouseholdScope Scope);

    #endregion
}
