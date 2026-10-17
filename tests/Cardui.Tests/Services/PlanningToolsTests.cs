using Cardui.Api.Data;
using Cardui.Api.Domain.Accounts;
using Cardui.Api.Domain.Planning;
using Cardui.Api.Domain.Recovery;
using Cardui.Api.Dtos.Account;
using Cardui.Api.Dtos.Allocation;
using Cardui.Api.Dtos.Debts;
using Cardui.Api.Dtos.Household;
using Cardui.Api.Dtos.Income;
using Cardui.Api.Dtos.Living;
using Cardui.Api.Dtos.Obligations;
using Cardui.Api.Dtos.Plan;
using Cardui.Api.Dtos.Savings;
using Cardui.Api.Exceptions;
using Cardui.Api.Models;
using Cardui.Api.Security;
using Cardui.Api.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Cardui.Tests.Services;

public class PlanningToolsTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 10, 16, 18, 0, 0, TimeSpan.Zero);

    private static readonly DateOnly Today = new(2026, 10, 16);

    [Fact]
    public async Task ReadSummaries_StayInsideTheHousehold()
    {
        await using var db = CreateDbContext();
        var household = await SeedAsync(db);

        var cash = await household.Tools.ReadSummaryAsync(PlanningSummaryKind.Cash);
        var debts = await household.Tools.ReadSummaryAsync(PlanningSummaryKind.Debts);
        var income = await household.Tools.ReadSummaryAsync(PlanningSummaryKind.Income);
        var bills = await household.Tools.ReadSummaryAsync(PlanningSummaryKind.Bills);
        var savings = await household.Tools.ReadSummaryAsync(PlanningSummaryKind.Savings);
        var living = await household.Tools.ReadSummaryAsync(PlanningSummaryKind.Living);
        var allocation = await household.Tools.ReadSummaryAsync(PlanningSummaryKind.Allocation);
        var plan = await household.Tools.ReadSummaryAsync(PlanningSummaryKind.Plan);
        var unknown = await Assert.ThrowsAsync<BadRequestException>(() =>
            household.Tools.ReadSummaryAsync((PlanningSummaryKind)99));

        Assert.Equal("Choose a summary.", unknown.Message);
        Assert.Equal(PlanningSummaryKind.Cash, cash.Kind);
        Assert.Equal(500m, cash.Cash!.Total);
        Assert.Equal(1, cash.Cash.AccountCount);
        Assert.Null(cash.Income);
        Assert.Null(cash.Bills);
        Assert.Contains(household.DebtId, debts.Debts!.Debts.Select(debt => debt.DebtId));
        Assert.DoesNotContain(household.OtherDebtId, debts.Debts.Debts.Select(debt => debt.DebtId));
        Assert.Equal("Paycheck", income.Income!.Single().Name);
        Assert.Equal("Rent", bills.Bills!.Single().Name);
        Assert.Equal("Emergency", savings.Savings!.Single().Name);
        Assert.Equal(100m, living.Living!.Contributions.Single().MonthlyAmount);
        Assert.Equal("Alex", living.Living.Contributions.Single().Name);
        Assert.False(allocation.Allocation!.IsSaved);
        Assert.Empty(plan.Plan!);
        Assert.Equal(0, await db.Conversations.CountAsync());
        await AssertUnchangedAsync(db, household);
    }

    [Fact]
    public async Task ProposeFacts_DoNotSave()
    {
        await using var db = CreateDbContext();
        var household = await SeedAsync(db);

        var income = await household.Tools.ProposeIncomeAsync(new UpsertIncomeSourceDto
        {
            Name = "  Side job  ",
            TakeHomeAmount = 300m,
            Cadence = IncomeCadence.Monthly,
            NextPaymentDate = Today,
            Reliability = IncomeReliability.Variable
        });
        var bill = await household.Tools.ProposeBillAsync(new UpsertObligationDto
        {
            Name = "Phone",
            Amount = 40m,
            Cadence = ObligationCadence.Monthly,
            NextDueDate = Today,
            Flexibility = ObligationFlexibility.Flexible
        });
        var debt = await household.Tools.ProposeDebtAsync(new UpsertDebtDto
        {
            Name = "Store card",
            Kind = DebtKind.Revolving,
            Balance = 80m,
            BalanceAsOf = Today
        });
        var savings = await household.Tools.ProposeSavingsAsync(new UpsertSavingsGoalDto
        {
            Kind = SavingsGoalKind.Sinking,
            Name = "Tires",
            TargetAmount = 400m,
            TargetDate = new DateOnly(2027, 1, 1),
            ReservedAmount = 0m
        });
        var living = await household.Tools.ProposeLivingContributionAsync(
            household.ContributorId,
            new UpsertLivingContributionDto { MonthlyAmount = 50m });
        var allocation = await household.Tools.ProposeAllocationAsync(new UpsertAllocationPreferenceDto
        {
            VariableIncomeDebtPercent = 40,
            VariableIncomeSavingsPercent = 20,
            WindfallDebtPercent = 50,
            WindfallSavingsPercent = 0
        });
        var blank = await Assert.ThrowsAsync<BadRequestException>(() =>
            household.Tools.ProposeIncomeAsync(new UpsertIncomeSourceDto()));
        var suggestion = await Assert.ThrowsAsync<BadRequestException>(() =>
            household.Tools.ProposeBillAsync(new UpsertObligationDto
            {
                Name = "Phone",
                Amount = 40m,
                Cadence = ObligationCadence.Monthly,
                NextDueDate = Today,
                Flexibility = ObligationFlexibility.Flexible,
                SuggestionKey = "phone"
            }));
        var split = await Assert.ThrowsAsync<BadRequestException>(() =>
            household.Tools.ProposeAllocationAsync(new UpsertAllocationPreferenceDto
            {
                VariableIncomeDebtPercent = 80,
                VariableIncomeSavingsPercent = 30
            }));

        Assert.False(income.Applied);
        Assert.Equal("Side job", income.Income.Name);
        Assert.False(bill.Applied);
        Assert.Equal("Phone", bill.Bill.Name);
        Assert.False(debt.Applied);
        Assert.Equal("Store card", debt.Debt.Name);
        Assert.False(savings.Applied);
        Assert.Equal("Tires", savings.Savings.Name);
        Assert.False(living.Applied);
        Assert.Equal(50m, living.MonthlyAmount);
        Assert.False(allocation.Applied);
        Assert.Equal(40, allocation.Allocation.VariableIncomeDebtPercent);
        Assert.Equal("An income source name is required.", blank.Message);
        Assert.Equal("A suggestion is not a bill until it is saved.", suggestion.Message);
        Assert.Equal(
            "Variable income: debt and savings together can't be more than 100 percent. The rest stays available.",
            split.Message);
        await AssertUnchangedAsync(db, household);
    }

    [Fact]
    public async Task ProposeFacts_StayInsideTheHousehold()
    {
        await using var db = CreateDbContext();
        var household = await SeedAsync(db);

        var contributor = await Assert.ThrowsAsync<BadRequestException>(() =>
            household.Tools.ProposeIncomeAsync(new UpsertIncomeSourceDto
            {
                Name = "Other pay",
                TakeHomeAmount = 100m,
                Cadence = IncomeCadence.Monthly,
                NextPaymentDate = Today,
                ContributorId = household.OtherContributorId,
                Reliability = IncomeReliability.Steady
            }));
        var account = await Assert.ThrowsAsync<BadRequestException>(() =>
            household.Tools.ProposeBillAsync(new UpsertObligationDto
            {
                Name = "Power",
                Amount = 60m,
                Cadence = ObligationCadence.Monthly,
                NextDueDate = Today,
                AccountId = household.OtherAccountId,
                Flexibility = ObligationFlexibility.Essential
            }));
        var card = await Assert.ThrowsAsync<BadRequestException>(() =>
            household.Tools.ProposeSavingsAsync(new UpsertSavingsGoalDto
            {
                Kind = SavingsGoalKind.Sinking,
                Name = "Trip",
                TargetAmount = 200m,
                TargetDate = new DateOnly(2027, 6, 1),
                ReservedAmount = 0m,
                AccountId = household.CreditAccountId,
                UseAccountBalance = true
            }));
        var followed = await Assert.ThrowsAsync<BadRequestException>(() =>
            household.Tools.ProposeSavingsAsync(new UpsertSavingsGoalDto
            {
                Kind = SavingsGoalKind.Sinking,
                Name = "Trip",
                TargetAmount = 200m,
                TargetDate = new DateOnly(2027, 6, 1),
                ReservedAmount = 0m,
                AccountId = household.CheckingAccountId,
                UseAccountBalance = true
            }));
        var missing = await Assert.ThrowsAsync<NotFoundException>(() =>
            household.Tools.ProposeLivingContributionAsync(
                household.OtherContributorId,
                new UpsertLivingContributionDto { MonthlyAmount = 10m }));

        Assert.Equal("Choose a contributor from this household.", contributor.Message);
        Assert.Equal("Choose an account from this household.", account.Message);
        Assert.Equal("Choose a cash account in this household.", card.Message);
        Assert.Equal("That account is already followed by another goal.", followed.Message);
        Assert.Equal("That contributor was not found.", missing.Message);
        await AssertUnchangedAsync(db, household);
    }

    [Fact]
    public async Task Calculations_DoNotSave()
    {
        await using var db = CreateDbContext();
        var household = await SeedAsync(db);
        var extra = new PlanScenarioQuery { MonthlyExtra = 25m };

        var calculated = await household.Tools.RequestCalculationAsync(extra);
        var compared = await household.Tools.CompareScenariosAsync(new PlanScenarioQuery(), extra);
        var offer = await household.Tools.CompareRestructuringAsync(new CompareRestructuringDto
        {
            Kind = RestructuringOfferKind.ReplacementLoan,
            DebtIds = [household.DebtId],
            Apr = 12m,
            TermMonths = 24,
            Fee = 0m,
            FeeFunding = RestructuringFeeFunding.Financed,
            UpfrontCash = 0m
        });
        var draft = await household.Tools.DraftPlanChangeAsync(extra, PayoffRolloverKind.Rollover);
        var otherDebt = await Assert.ThrowsAsync<BadRequestException>(() =>
            household.Tools.RequestCalculationAsync(new PlanScenarioQuery
            {
                Priority = PayoffPriorityChoice.Constraint,
                ConstraintDebtId = household.OtherDebtId,
                ConstraintKind = PayoffConstraintKind.PayFirst
            }));
        var otherOffer = await Assert.ThrowsAsync<BadRequestException>(() =>
            household.Tools.CompareRestructuringAsync(new CompareRestructuringDto
            {
                Kind = RestructuringOfferKind.ReplacementLoan,
                DebtIds = [household.OtherDebtId],
                Apr = 12m,
                TermMonths = 24,
                Fee = 0m,
                FeeFunding = RestructuringFeeFunding.Financed
            }));
        var unsupported = await Assert.ThrowsAsync<BadRequestException>(() =>
            household.Tools.RequestCalculationAsync(new PlanScenarioQuery { IncomeLossMonths = 99 }));

        Assert.Equal(25m, calculated.MonthlyExtra);
        Assert.False(compared.Applied);
        Assert.Equal(0m, compared.Baseline.MonthlyExtra);
        Assert.Equal(25m, compared.Alternative.MonthlyExtra);
        Assert.Equal(RestructuringOfferKind.ReplacementLoan, offer.Kind);
        Assert.Equal(household.DebtId, offer.Debts.Single().DebtId);
        Assert.False(draft.Applied);
        Assert.Equal(PayoffRolloverKind.Rollover, draft.Plan.Kind);
        Assert.Equal(25m, draft.Plan.Scenario.MonthlyExtra);
        Assert.Equal("Choose a debt in this plan.", otherDebt.Message);
        Assert.Equal("Choose debts from this household.", otherOffer.Message);
        Assert.Equal("Income loss has to be from 0 to 18 months.", unsupported.Message);
        await AssertUnchangedAsync(db, household);
    }

    [Fact]
    public async Task Tools_RequireAHousehold()
    {
        await using var db = CreateDbContext();
        var tools = CreateTools(db, new HouseholdScope(), new FakeTimeProvider(Now));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            tools.ReadSummaryAsync(PlanningSummaryKind.Cash));

        Assert.Equal("A household scope is required.", error.Message);
        Assert.Equal(0, await db.PlanVersions.CountAsync());
    }

    #region Private Methods

    private static async Task AssertUnchangedAsync(CarduiDBContext db, HouseholdFacts household)
    {
        Assert.Equal(household.IncomeCount, await db.IncomeSources.CountAsync());
        Assert.Equal(household.BillCount, await db.Obligations.CountAsync());
        Assert.Equal(household.DebtCount, await db.Debts.CountAsync());
        Assert.Equal(household.SavingsCount, await db.SavingsGoals.CountAsync());
        Assert.Equal(household.AllocationCount, await db.AllocationPreferences.CountAsync());
        Assert.Equal(household.PlanCount, await db.PlanVersions.CountAsync());
        Assert.Equal(household.AccountCount, await db.Accounts.CountAsync());
        Assert.Equal(0, await db.Conversations.CountAsync());
        var contribution = await db.HouseholdContributors
            .Where(contributor => contributor.Id == household.ContributorId)
            .Select(contributor => contributor.MonthlyContribution)
            .SingleAsync();
        Assert.Equal(100m, contribution);
    }

    private static async Task<HouseholdFacts> SeedAsync(CarduiDBContext db)
    {
        var time = new FakeTimeProvider(Now);
        var scope = new HouseholdScope();
        var households = new HouseholdsService(db, time);
        var home = await households.GetOrCreateForOwnerAsync("owner_home");
        var other = await households.GetOrCreateForOwnerAsync("owner_other");

        scope.Bind(other.Id);
        var otherProfile = new FinancialProfileService(db, time, scope);
        var otherContributor = await otherProfile.AddContributorAsync(
            new UpsertHouseholdContributorDto { Name = "Blake", IsVisible = true });
        var otherAccounts = new AccountsService(db, time, scope);
        var otherAccount = await otherAccounts.CreateManualAccountAsync(CashAccount("Other checking", 900m));
        var otherDebts = new DebtsService(db, time, scope);
        var otherDebt = await otherDebts.CreateAsync(Card("Other card"));

        scope.Bind(home.Id);
        var profile = new FinancialProfileService(db, time, scope);
        var contributor = await profile.AddContributorAsync(
            new UpsertHouseholdContributorDto { Name = "Alex", IsVisible = true });
        var accounts = new AccountsService(db, time, scope);
        var checking = await accounts.CreateManualAccountAsync(CashAccount("Checking", 500m));
        var credit = await accounts.CreateManualAccountAsync(new CreateManualAccountDto
        {
            Name = "Card account",
            Type = AccountTypes.Credit,
            IsoCurrencyCode = "USD",
            OpeningBalance = 100m,
            OpeningBalanceDate = new DateOnly(2026, 10, 1)
        });
        var incomes = new IncomeSourcesService(db, time, scope);
        await incomes.CreateAsync(new UpsertIncomeSourceDto
        {
            Name = "Paycheck",
            TakeHomeAmount = 2000m,
            Cadence = IncomeCadence.Monthly,
            NextPaymentDate = new DateOnly(2026, 10, 20),
            ContributorId = contributor.Id,
            Reliability = IncomeReliability.Steady
        });
        var obligations = new ObligationsService(db, time, scope);
        await obligations.CreateAsync(new UpsertObligationDto
        {
            Name = "Rent",
            Amount = 900m,
            Cadence = ObligationCadence.Monthly,
            NextDueDate = new DateOnly(2026, 10, 20),
            Flexibility = ObligationFlexibility.Essential
        });
        var debts = new DebtsService(db, time, scope);
        var debt = await debts.CreateAsync(Card("Card"));
        var savings = new SavingsGoalsService(db, time, scope);
        await savings.CreateAsync(new UpsertSavingsGoalDto
        {
            Kind = SavingsGoalKind.Emergency,
            TargetAmount = 1000m,
            TargetDate = new DateOnly(2027, 10, 1),
            ReservedAmount = 0m,
            AccountId = checking.Id,
            UseAccountBalance = true
        });
        var living = new LivingService(db, incomes, obligations, debts, savings, time, scope);
        await living.SetContributionAsync(
            contributor.Id,
            new UpsertLivingContributionDto { MonthlyAmount = 100m });

        return new HouseholdFacts(
            CreateTools(db, scope, time),
            contributor.Id,
            otherContributor.Id,
            checking.Id,
            credit.Id,
            otherAccount.Id,
            debt.Id,
            otherDebt.Id,
            await db.IncomeSources.CountAsync(),
            await db.Obligations.CountAsync(),
            await db.Debts.CountAsync(),
            await db.SavingsGoals.CountAsync(),
            await db.AllocationPreferences.CountAsync(),
            await db.PlanVersions.CountAsync(),
            await db.Accounts.CountAsync());
    }

    private static UpsertDebtDto Card(string name)
    {
        return new UpsertDebtDto
        {
            Name = name,
            Kind = DebtKind.Revolving,
            Balance = 800m,
            BalanceAsOf = new DateOnly(2026, 10, 1),
            Apr = 20m,
            MinimumPayment = 25m
        };
    }

    private static CreateManualAccountDto CashAccount(string name, decimal balance)
    {
        return new CreateManualAccountDto
        {
            Name = name,
            Type = AccountTypes.Depository,
            IsoCurrencyCode = "USD",
            OpeningBalance = balance,
            OpeningBalanceDate = new DateOnly(2026, 10, 1)
        };
    }

    private static PlanningTools CreateTools(
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
        return new PlanningTools(
            accounts,
            debts,
            incomes,
            obligations,
            savings,
            living,
            new AllocationService(db, time, scope),
            new FinancialProfileService(db, time, scope),
            new PlanService(debts, incomes, obligations, accounts, savings, living, time, scope),
            new SavedPlanService(
                db,
                new PlanService(debts, incomes, obligations, accounts, savings, living, time, scope),
                time,
                scope),
            new RestructuringService(debts, accounts, savings, time, scope),
            scope);
    }

    private static CarduiDBContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CarduiDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new CarduiDBContext(options);
    }

    private sealed record HouseholdFacts(
        PlanningTools Tools,
        Guid ContributorId,
        Guid OtherContributorId,
        Guid CheckingAccountId,
        Guid CreditAccountId,
        Guid OtherAccountId,
        Guid DebtId,
        Guid OtherDebtId,
        int IncomeCount,
        int BillCount,
        int DebtCount,
        int SavingsCount,
        int AllocationCount,
        int PlanCount,
        int AccountCount);

    #endregion
}
