using Cardui.Api.Data;
using Cardui.Api.Domain.Accounts;
using Cardui.Api.Domain.Planning;
using Cardui.Api.Dtos.Account;
using Cardui.Api.Dtos.Debts;
using Cardui.Api.Dtos.Income;
using Cardui.Api.Dtos.Obligations;
using Cardui.Api.Exceptions;
using Cardui.Api.Models;
using Cardui.Api.Security;
using Cardui.Api.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Cardui.Tests.Services;

public class PlanningQuestionsTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 10, 16, 18, 0, 0, TimeSpan.Zero);

    private static readonly DateOnly Today = new(2026, 10, 16);

    [Fact]
    public async Task Select_StartsWithCashAndTheNextPaycheck()
    {
        await using var db = CreateDbContext();
        var household = await SeedAsync(db);

        var questions = await household.Questions.SelectAsync();

        Assert.Equal(
            [PlanningQuestionTopic.CashOnHand, PlanningQuestionTopic.NextPaycheck],
            questions.Select(question => question.Topic));
        Assert.Equal(0, await db.Accounts.CountAsync());
        Assert.Equal(0, await db.IncomeSources.CountAsync());
        Assert.Equal(0, await db.Obligations.CountAsync());
        Assert.Equal(0, await db.Debts.CountAsync());
        Assert.Equal(0, await db.Conversations.CountAsync());
    }

    [Fact]
    public async Task Select_AsksTwoDebtPaymentsAndLeavesTheThird()
    {
        await using var db = CreateDbContext();
        var household = await SeedAsync(db);
        await household.Accounts.CreateManualAccountAsync(CashAccount("Checking", 500m));
        await household.Incomes.CreateAsync(Pay("Paycheck", IncomeReliability.Steady));
        await household.Bills.CreateAsync(Bill("Rent", ObligationFlexibility.Essential));
        var alpha = await household.Debts.CreateAsync(BlankDebt("Alpha"));
        var beta = await household.Debts.CreateAsync(BlankDebt("Beta"));
        var gamma = await household.Debts.CreateAsync(BlankDebt("Gamma"));

        var questions = await household.Questions.SelectAsync();

        Assert.Equal(2, questions.Count);
        Assert.Equal(alpha.Id, questions[0].DebtId);
        Assert.Equal("What is the minimum payment on Alpha, and when is it due?", questions[0].Text);
        Assert.Equal(beta.Id, questions[1].DebtId);
        Assert.DoesNotContain(questions, question => question.DebtId == gamma.Id);
        Assert.Equal(0, await db.Conversations.CountAsync());
        Assert.Equal(3, await db.Debts.CountAsync(debt => debt.MinimumPayment == null));
    }

    [Fact]
    public async Task Select_AsksTheEssentialBillWhenTheOnlyBillIsFlexible()
    {
        await using var db = CreateDbContext();
        var household = await SeedAsync(db);
        await household.Accounts.CreateManualAccountAsync(CashAccount("Checking", 500m));
        await household.Incomes.CreateAsync(Pay("Paycheck", IncomeReliability.Steady));
        await household.Bills.CreateAsync(Bill("Hobby", ObligationFlexibility.Flexible));

        var questions = await household.Questions.SelectAsync();

        var question = Assert.Single(questions);
        Assert.Equal(PlanningQuestionTopic.NextEssentialBill, question.Topic);
        Assert.Equal(1, await db.Obligations.CountAsync());
    }

    [Fact]
    public async Task Select_AsksWhetherVariablePayShouldBeLeftOut()
    {
        await using var db = CreateDbContext();
        var household = await SeedAsync(db);
        await household.Accounts.CreateManualAccountAsync(CashAccount("Checking", 500m));
        await household.Incomes.CreateAsync(Pay("Side job", IncomeReliability.Variable));
        await household.Bills.CreateAsync(Bill("Rent", ObligationFlexibility.Essential));

        var questions = await household.Questions.SelectAsync();

        var question = Assert.Single(questions);
        Assert.Equal(PlanningQuestionTopic.UnreliablePay, question.Topic);
        Assert.Equal(
            "Is the next payment from Side job expected to arrive, or should the plan leave it out?",
            question.Text);
        Assert.Equal(IncomeReliability.Variable, (await household.Incomes.GetIncomeSourcesAsync()).Single().Reliability);
    }

    [Fact]
    public async Task Select_TreatsAZeroMinimumAsRecorded()
    {
        await using var db = CreateDbContext();
        var household = await SeedAsync(db);
        await household.Accounts.CreateManualAccountAsync(CashAccount("Checking", 500m));
        await household.Incomes.CreateAsync(Pay("Paycheck", IncomeReliability.Steady));
        await household.Bills.CreateAsync(Bill("Rent", ObligationFlexibility.Essential));
        var debt = await household.Debts.CreateAsync(new UpsertDebtDto
        {
            Name = "Card",
            Kind = DebtKind.Revolving,
            Balance = 800m,
            BalanceAsOf = Today,
            Apr = 20m,
            MinimumPayment = 0m,
            NextDueDate = new DateOnly(2026, 10, 20)
        });

        var questions = await household.Questions.SelectAsync();

        var question = Assert.Single(questions);
        Assert.Equal(PlanningQuestionTopic.LivingSpending, question.Topic);
        Assert.Equal(0m, (await household.Debts.GetDebtsAsync()).Single(item => item.Id == debt.Id).MinimumPayment);
    }

    [Fact]
    public async Task LeaveUnknown_SkipsThatDetailAndDoesNotSaveAFact()
    {
        await using var db = CreateDbContext();
        var household = await SeedAsync(db);
        var conversation = await household.Conversations.StartAsync();

        var noted = await household.Questions.LeaveUnknownAsync(conversation.Id, "CashOnHand");
        var questions = await household.Questions.SelectAsync();

        Assert.Equal("Question:CashOnHand", noted.Preferences.Single().Key);
        Assert.Equal(PlanningQuestionRules.Unknown, noted.Preferences.Single().Value);
        Assert.Empty(noted.Questions);
        Assert.Equal(
            [PlanningQuestionTopic.NextPaycheck, PlanningQuestionTopic.NextEssentialBill],
            questions.Select(question => question.Topic));
        Assert.Equal(0, await db.Accounts.CountAsync());
        Assert.Equal(0, await db.IncomeSources.CountAsync());
        Assert.Equal(0, await db.Obligations.CountAsync());
        Assert.Equal(0, await db.Debts.CountAsync());
    }

    [Fact]
    public async Task LeaveUnknown_SkipsADebtPaymentWithoutFillingItIn()
    {
        await using var db = CreateDbContext();
        var household = await SeedAsync(db);
        await household.Accounts.CreateManualAccountAsync(CashAccount("Checking", 500m));
        await household.Incomes.CreateAsync(Pay("Paycheck", IncomeReliability.Steady));
        await household.Bills.CreateAsync(Bill("Rent", ObligationFlexibility.Essential));
        var debt = await household.Debts.CreateAsync(BlankDebt("Card"));
        var conversation = await household.Conversations.StartAsync();

        await household.Questions.LeaveUnknownAsync(
            conversation.Id,
            PlanningQuestionRules.Key(PlanningQuestionTopic.DebtPayment, debt.Id));
        var questions = await household.Questions.SelectAsync();
        var saved = (await household.Debts.GetDebtsAsync()).Single();

        Assert.Equal(
            [PlanningQuestionTopic.LivingSpending, PlanningQuestionTopic.DebtApr],
            questions.Select(question => question.Topic));
        Assert.Equal(debt.Id, questions[1].DebtId);
        Assert.Null(saved.MinimumPayment);
        Assert.Null(saved.NextDueDate);
        Assert.Null(saved.Apr);
    }

    [Fact]
    public async Task Select_KeepsAskingWhenTheNoteIsNotUnknown()
    {
        await using var db = CreateDbContext();
        var household = await SeedAsync(db);
        var conversation = await household.Conversations.StartAsync();
        await household.Conversations.SetPreferenceAsync(
            conversation.Id,
            "CashOnHand",
            PlanningQuestionRules.Unknown);
        var ordinary = await household.Questions.SelectAsync();

        await household.Conversations.SetPreferenceAsync(conversation.Id, "Question:CashOnHand", "About 500");
        var noted = await household.Questions.SelectAsync();

        Assert.Equal(PlanningQuestionTopic.CashOnHand, ordinary[0].Topic);
        Assert.Equal(PlanningQuestionTopic.CashOnHand, noted[0].Topic);
    }

    [Fact]
    public async Task LeaveUnknown_StaysInsideTheHousehold()
    {
        await using var db = CreateDbContext();
        var household = await SeedAsync(db);
        var conversation = await household.Conversations.StartAsync();
        var otherScope = new HouseholdScope();
        otherScope.Bind(household.OtherHouseholdId);
        var otherDebt = await new DebtsService(db, household.Time, otherScope).CreateAsync(BlankDebt("Other card"));

        var missing = await Assert.ThrowsAsync<NotFoundException>(() =>
            household.Questions.LeaveUnknownAsync(
                conversation.Id,
                PlanningQuestionRules.Key(PlanningQuestionTopic.DebtPayment, otherDebt.Id)));
        var questions = await household.Questions.SelectAsync();

        Assert.Equal("That debt was not found.", missing.Message);
        Assert.DoesNotContain(questions, question => question.DebtId == otherDebt.Id);
        Assert.Null(await db.Debts.Where(debt => debt.Id == otherDebt.Id).Select(debt => debt.MinimumPayment).SingleAsync());
        Assert.Equal(0, await db.ConversationPreferences.CountAsync());
    }

    [Fact]
    public async Task LeaveUnknown_RejectsAClosedConversation()
    {
        await using var db = CreateDbContext();
        var household = await SeedAsync(db);
        var conversation = await household.Conversations.StartAsync();
        await household.Conversations.CloseAsync(conversation.Id);

        var error = await Assert.ThrowsAsync<BadRequestException>(() =>
            household.Questions.LeaveUnknownAsync(conversation.Id, "CashOnHand"));

        Assert.Equal("This conversation is closed.", error.Message);
        Assert.Equal(0, await db.ConversationPreferences.CountAsync());
    }

    [Theory]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("DebtPayment")]
    public async Task LeaveUnknown_RejectsAKeyThisRuleDoesNotDefine(string key)
    {
        await using var db = CreateDbContext();
        var household = await SeedAsync(db);
        var conversation = await household.Conversations.StartAsync();

        var error = await Assert.ThrowsAsync<BadRequestException>(() =>
            household.Questions.LeaveUnknownAsync(conversation.Id, key));

        Assert.Equal("Choose a question.", error.Message);
        Assert.Equal(0, await db.ConversationPreferences.CountAsync());
    }

    [Fact]
    public async Task Select_RequiresAHousehold()
    {
        await using var db = CreateDbContext();
        var questions = CreateQuestions(db, new HouseholdScope(), new FakeTimeProvider(Now));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => questions.SelectAsync());

        Assert.Equal("A household scope is required.", error.Message);
    }

    #region Private Methods

    private static async Task<HouseholdFacts> SeedAsync(CarduiDBContext db)
    {
        var time = new FakeTimeProvider(Now);
        var households = new HouseholdsService(db, time);
        var home = await households.GetOrCreateForOwnerAsync("owner_home");
        var other = await households.GetOrCreateForOwnerAsync("owner_other");
        var scope = new HouseholdScope();
        scope.Bind(home.Id);
        return new HouseholdFacts(
            CreateQuestions(db, scope, time),
            new AccountsService(db, time, scope),
            new IncomeSourcesService(db, time, scope),
            new ObligationsService(db, time, scope),
            new DebtsService(db, time, scope),
            new ConversationService(db, time, scope),
            other.Id,
            time);
    }

    private static PlanningQuestions CreateQuestions(
        CarduiDBContext db,
        HouseholdScope scope,
        TimeProvider time)
    {
        var incomes = new IncomeSourcesService(db, time, scope);
        var obligations = new ObligationsService(db, time, scope);
        var debts = new DebtsService(db, time, scope);
        var savings = new SavingsGoalsService(db, time, scope);
        return new PlanningQuestions(
            new AccountsService(db, time, scope),
            incomes,
            obligations,
            debts,
            new LivingService(db, incomes, obligations, debts, savings, time, scope),
            new ConversationService(db, time, scope),
            time,
            scope);
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

    private static UpsertIncomeSourceDto Pay(string name, IncomeReliability reliability)
    {
        return new UpsertIncomeSourceDto
        {
            Name = name,
            TakeHomeAmount = 2000m,
            Cadence = IncomeCadence.Monthly,
            NextPaymentDate = new DateOnly(2026, 10, 20),
            Reliability = reliability
        };
    }

    private static UpsertObligationDto Bill(string name, ObligationFlexibility flexibility)
    {
        return new UpsertObligationDto
        {
            Name = name,
            Amount = 900m,
            Cadence = ObligationCadence.Monthly,
            NextDueDate = new DateOnly(2026, 10, 20),
            Flexibility = flexibility
        };
    }

    private static UpsertDebtDto BlankDebt(string name)
    {
        return new UpsertDebtDto
        {
            Name = name,
            Kind = DebtKind.Revolving,
            Balance = 100m,
            BalanceAsOf = Today
        };
    }

    private static CarduiDBContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CarduiDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new CarduiDBContext(options);
    }

    private sealed record HouseholdFacts(
        PlanningQuestions Questions,
        AccountsService Accounts,
        IncomeSourcesService Incomes,
        ObligationsService Bills,
        DebtsService Debts,
        ConversationService Conversations,
        Guid OtherHouseholdId,
        FakeTimeProvider Time);

    #endregion
}
