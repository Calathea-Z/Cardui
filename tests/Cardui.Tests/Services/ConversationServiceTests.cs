using Cardui.Api.Data;
using Cardui.Api.Domain;
using Cardui.Api.Exceptions;
using Cardui.Api.Models;
using Cardui.Api.Security;
using Cardui.Api.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Cardui.Tests.Services;

public class ConversationServiceTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 10, 16, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Start_OpensOneConversationForTheHousehold()
    {
        await using var dbContext = CreateDbContext();
        var householdId = await CreateHouseholdAsync(dbContext, "user_owner");
        var service = CreateService(dbContext, Bind(householdId), new FakeTimeProvider(Now));

        Assert.Null(await service.GetOpenAsync());

        var started = await service.StartAsync();
        var again = await service.StartAsync();

        Assert.Equal(started.Id, again.Id);
        Assert.Equal(ConversationStatus.Open, again.Status);
        Assert.Equal(Now, again.CreatedAt);
        Assert.Equal(Now, again.UpdatedAt);
        Assert.Null(again.ClosedAt);
        Assert.Null(again.Summary);
        Assert.Empty(again.Messages);
        Assert.Empty(again.Questions);
        Assert.Empty(again.Preferences);
        Assert.Empty(again.PlanVersions);
        Assert.Equal(started.Id, (await service.GetOpenAsync())!.Id);
        Assert.Equal(1, await dbContext.Conversations.CountAsync());
    }

    [Fact]
    public async Task Resume_KeepsTheThreadAcrossDays()
    {
        await using var dbContext = CreateDbContext();
        var time = new FakeTimeProvider(Now);
        var householdId = await CreateHouseholdAsync(dbContext, "user_owner");
        var planId = await SeedPlanAsync(dbContext, householdId);
        var service = CreateService(dbContext, Bind(householdId), time);
        var started = await service.StartAsync();

        await service.AppendMessageAsync(
            started.Id,
            ConversationMessageRole.User,
            "I don't know where to begin.");
        await service.ReplaceSummaryAsync(started.Id, "Starting with cash and the card.");
        await service.AddQuestionAsync(started.Id, "Which bill is due first?");
        await service.SetPreferenceAsync(started.Id, "Cash to keep", "One month of bills");
        await service.LinkPlanVersionAsync(started.Id, planId);
        var savedAt = (await service.GetAsync(started.Id)).UpdatedAt;

        time.Advance(TimeSpan.FromDays(3));
        var resumed = await service.GetOpenAsync();

        Assert.NotNull(resumed);
        Assert.Equal(started.Id, resumed.Id);
        Assert.Equal(savedAt, resumed.UpdatedAt);
        Assert.Equal("Starting with cash and the card.", resumed.Summary);
        Assert.Equal("I don't know where to begin.", resumed.Messages.Single().Text);
        Assert.Equal("Which bill is due first?", resumed.Questions.Single().Text);
        Assert.Equal("Cash to keep", resumed.Preferences.Single().Key);
        Assert.Equal("One month of bills", resumed.Preferences.Single().Value);
        Assert.Equal(planId, resumed.PlanVersions.Single().PlanVersionId);
        Assert.Equal(1, resumed.PlanVersions.Single().Number);
        Assert.Equal(PlanVersionStatus.Draft, resumed.PlanVersions.Single().Status);
    }

    [Fact]
    public async Task Close_KeepsTheThreadAndTheNextStartIsEmpty()
    {
        await using var dbContext = CreateDbContext();
        var time = new FakeTimeProvider(Now);
        var householdId = await CreateHouseholdAsync(dbContext, "user_owner");
        var service = CreateService(dbContext, Bind(householdId), time);
        var started = await service.StartAsync();
        await service.AppendMessageAsync(started.Id, ConversationMessageRole.User, "Rent is Friday.");
        await service.ReplaceSummaryAsync(started.Id, "Rent is the open question.");

        var closed = await service.CloseAsync(started.Id);
        time.Advance(TimeSpan.FromDays(1));
        var again = await service.CloseAsync(started.Id);

        Assert.Equal(ConversationStatus.Closed, closed.Status);
        Assert.Equal(Now, closed.ClosedAt);
        Assert.Equal(closed.ClosedAt, again.ClosedAt);
        Assert.Equal(closed.UpdatedAt, again.UpdatedAt);
        Assert.Null(await service.GetOpenAsync());

        var next = await service.StartAsync();
        Assert.NotEqual(started.Id, next.Id);
        Assert.Equal(ConversationStatus.Open, next.Status);
        Assert.Empty(next.Messages);
        Assert.Null(next.Summary);

        var previous = await service.GetAsync(started.Id);
        Assert.Equal(ConversationStatus.Closed, previous.Status);
        Assert.Equal("Rent is Friday.", previous.Messages.Single().Text);
        Assert.Equal("Rent is the open question.", previous.Summary);
    }

    [Fact]
    public async Task AppendMessage_StoresTurnsInOrder()
    {
        await using var dbContext = CreateDbContext();
        var time = new FakeTimeProvider(Now);
        var householdId = await CreateHouseholdAsync(dbContext, "user_owner");
        var service = CreateService(dbContext, Bind(householdId), time);
        var started = await service.StartAsync();

        var blank = await Assert.ThrowsAsync<BadRequestException>(() =>
            service.AppendMessageAsync(started.Id, ConversationMessageRole.User, "  "));
        var tooLong = await Assert.ThrowsAsync<BadRequestException>(() =>
            service.AppendMessageAsync(
                started.Id,
                ConversationMessageRole.User,
                new string('a', ConversationMessage.TextMaxLength + 1)));
        var role = await Assert.ThrowsAsync<BadRequestException>(() =>
            service.AppendMessageAsync(started.Id, (ConversationMessageRole)9, "Hello"));

        time.Advance(TimeSpan.FromMinutes(1));
        await service.AppendMessageAsync(started.Id, ConversationMessageRole.User, "  Hello  ");
        time.Advance(TimeSpan.FromMinutes(1));
        var saved = await service.AppendMessageAsync(
            started.Id,
            ConversationMessageRole.Assistant,
            "We can start with cash.");

        Assert.Equal("Enter a message.", blank.Message);
        Assert.Equal(
            $"A message must be {ConversationMessage.TextMaxLength} characters or fewer.",
            tooLong.Message);
        Assert.Equal("A message is from the person or the assistant.", role.Message);
        Assert.Equal([1, 2], saved.Messages.Select(message => message.Sequence));
        Assert.Equal(ConversationMessageRole.User, saved.Messages[0].Role);
        Assert.Equal("Hello", saved.Messages[0].Text);
        Assert.Equal(ConversationMessageRole.Assistant, saved.Messages[1].Role);
        Assert.Equal(2, await dbContext.ConversationMessages.CountAsync());
    }

    [Fact]
    public async Task ReplaceSummary_ReplacesAndClears()
    {
        await using var dbContext = CreateDbContext();
        var time = new FakeTimeProvider(Now);
        var householdId = await CreateHouseholdAsync(dbContext, "user_owner");
        var service = CreateService(dbContext, Bind(householdId), time);
        var started = await service.StartAsync();

        var blank = await Assert.ThrowsAsync<BadRequestException>(() =>
            service.ReplaceSummaryAsync(started.Id, "   "));
        var saved = await service.ReplaceSummaryAsync(started.Id, "  Cash first.  ");
        time.Advance(TimeSpan.FromMinutes(1));
        var same = await service.ReplaceSummaryAsync(started.Id, "Cash first.");
        var cleared = await service.ReplaceSummaryAsync(started.Id, null);

        Assert.Equal("Enter a summary.", blank.Message);
        Assert.Equal("Cash first.", saved.Summary);
        Assert.Equal(saved.UpdatedAt, same.UpdatedAt);
        Assert.Null(cleared.Summary);
        Assert.True(cleared.UpdatedAt > saved.UpdatedAt);
    }

    [Fact]
    public async Task Questions_StayOpenUntilAnswered()
    {
        await using var dbContext = CreateDbContext();
        var time = new FakeTimeProvider(Now);
        var householdId = await CreateHouseholdAsync(dbContext, "user_owner");
        var service = CreateService(dbContext, Bind(householdId), time);
        var started = await service.StartAsync();

        var first = await service.AddQuestionAsync(started.Id, "Which bill is due first?");
        time.Advance(TimeSpan.FromMinutes(1));
        var duplicate = await service.AddQuestionAsync(started.Id, "  which bill is due first? ");
        time.Advance(TimeSpan.FromMinutes(1));
        var second = await service.AddQuestionAsync(started.Id, "What is the card APR?");

        Assert.Equal(first.UpdatedAt, duplicate.UpdatedAt);
        Assert.Equal(
            ["Which bill is due first?", "What is the card APR?"],
            second.Questions.Select(question => question.Text));

        var answered = await service.AnswerQuestionAsync(started.Id, second.Questions[0].Id);
        time.Advance(TimeSpan.FromMinutes(1));
        var repeated = await service.AnswerQuestionAsync(started.Id, second.Questions[0].Id);
        var askedAgain = await service.AddQuestionAsync(started.Id, "Which bill is due first?");

        Assert.Equal(["What is the card APR?"], answered.Questions.Select(question => question.Text));
        Assert.Equal(answered.UpdatedAt, repeated.UpdatedAt);
        Assert.Equal(
            ["What is the card APR?", "Which bill is due first?"],
            askedAgain.Questions.Select(question => question.Text));
        Assert.Equal(3, await dbContext.ConversationQuestions.CountAsync());
        Assert.Equal(
            1,
            await dbContext.ConversationQuestions.CountAsync(question => question.AnsweredAt != null));

        var missing = await Assert.ThrowsAsync<NotFoundException>(() =>
            service.AnswerQuestionAsync(started.Id, Guid.NewGuid()));
        Assert.Equal("That question was not found.", missing.Message);
    }

    [Fact]
    public async Task Preferences_RememberANoteWithoutChangingThePlan()
    {
        await using var dbContext = CreateDbContext();
        var time = new FakeTimeProvider(Now);
        var householdId = await CreateHouseholdAsync(dbContext, "user_owner");
        var planId = await SeedPlanAsync(dbContext, householdId);
        var service = CreateService(dbContext, Bind(householdId), time);
        var started = await service.StartAsync();

        var saved = await service.SetPreferenceAsync(started.Id, "  Cash to keep  ", " One month ");
        time.Advance(TimeSpan.FromMinutes(1));
        var same = await service.SetPreferenceAsync(started.Id, "cash to keep", "One month");
        var replaced = await service.SetPreferenceAsync(started.Id, "CASH TO KEEP", "Two weeks");
        var removed = await service.RemovePreferenceAsync(started.Id, "Cash To Keep");

        Assert.Equal("Cash to keep", saved.Preferences.Single().Key);
        Assert.Equal("One month", saved.Preferences.Single().Value);
        Assert.Equal(saved.UpdatedAt, same.UpdatedAt);
        Assert.Equal("Cash to keep", replaced.Preferences.Single().Key);
        Assert.Equal("Two weeks", replaced.Preferences.Single().Value);
        Assert.Empty(removed.Preferences);
        Assert.Equal(0, await dbContext.ConversationPreferences.CountAsync());
        Assert.Equal(0, await dbContext.AllocationPreferences.CountAsync());

        var plan = await dbContext.PlanVersions.SingleAsync(version => version.Id == planId);
        Assert.Equal(PlanVersionStatus.Draft, plan.Status);
        Assert.Equal("{}", plan.Document);

        var missing = await Assert.ThrowsAsync<NotFoundException>(() =>
            service.RemovePreferenceAsync(started.Id, "Cash to keep"));
        Assert.Equal("That preference was not found.", missing.Message);
    }

    [Fact]
    public async Task LinkPlanVersion_RequiresAPlanInTheHousehold()
    {
        await using var dbContext = CreateDbContext();
        var time = new FakeTimeProvider(Now);
        var householdId = await CreateHouseholdAsync(dbContext, "user_owner");
        var otherId = await CreateHouseholdAsync(dbContext, "user_other");
        var planId = await SeedPlanAsync(dbContext, householdId);
        var otherPlanId = await SeedPlanAsync(dbContext, otherId);
        var service = CreateService(dbContext, Bind(householdId), time);
        var started = await service.StartAsync();

        time.Advance(TimeSpan.FromMinutes(1));
        var linked = await service.LinkPlanVersionAsync(started.Id, planId);
        time.Advance(TimeSpan.FromMinutes(1));
        var again = await service.LinkPlanVersionAsync(started.Id, planId);
        var foreign = await Assert.ThrowsAsync<NotFoundException>(() =>
            service.LinkPlanVersionAsync(started.Id, otherPlanId));
        var unlinked = await service.UnlinkPlanVersionAsync(started.Id, planId);
        var missing = await Assert.ThrowsAsync<NotFoundException>(() =>
            service.UnlinkPlanVersionAsync(started.Id, planId));

        Assert.Equal(planId, linked.PlanVersions.Single().PlanVersionId);
        Assert.Equal(linked.UpdatedAt, again.UpdatedAt);
        Assert.Equal("That saved plan was not found.", foreign.Message);
        Assert.Empty(unlinked.PlanVersions);
        Assert.Equal("That saved plan is not linked to this conversation.", missing.Message);
        Assert.Equal(2, await dbContext.PlanVersions.CountAsync());
        Assert.Equal(0, await dbContext.ConversationPlanLinks.CountAsync());

        var plan = await dbContext.PlanVersions.SingleAsync(version => version.Id == planId);
        Assert.Equal(PlanVersionStatus.Draft, plan.Status);
        Assert.Equal("{}", plan.Document);
    }

    [Fact]
    public async Task ClosedConversation_RejectsLaterChanges()
    {
        await using var dbContext = CreateDbContext();
        var householdId = await CreateHouseholdAsync(dbContext, "user_owner");
        var planId = await SeedPlanAsync(dbContext, householdId);
        var service = CreateService(dbContext, Bind(householdId), new FakeTimeProvider(Now));
        var started = await service.StartAsync();
        await service.AppendMessageAsync(started.Id, ConversationMessageRole.User, "Rent is Friday.");
        var withQuestion = await service.AddQuestionAsync(started.Id, "Which card first?");
        await service.SetPreferenceAsync(started.Id, "Cash to keep", "One month of bills");
        await service.LinkPlanVersionAsync(started.Id, planId);
        await service.CloseAsync(started.Id);

        await AssertClosed(() =>
            service.AppendMessageAsync(started.Id, ConversationMessageRole.Assistant, "Later"));
        await AssertClosed(() => service.ReplaceSummaryAsync(started.Id, "Summary"));
        await AssertClosed(() => service.AddQuestionAsync(started.Id, "Another question"));
        await AssertClosed(() =>
            service.AnswerQuestionAsync(started.Id, withQuestion.Questions.Single().Id));
        await AssertClosed(() =>
            service.SetPreferenceAsync(started.Id, "Cash to keep", "More"));
        await AssertClosed(() => service.RemovePreferenceAsync(started.Id, "Cash to keep"));
        await AssertClosed(() => service.LinkPlanVersionAsync(started.Id, planId));
        await AssertClosed(() => service.UnlinkPlanVersionAsync(started.Id, planId));

        var read = await service.GetAsync(started.Id);
        Assert.Equal("Rent is Friday.", read.Messages.Single().Text);
        Assert.Equal("Which card first?", read.Questions.Single().Text);
        Assert.Equal("One month of bills", read.Preferences.Single().Value);
        Assert.Equal(planId, read.PlanVersions.Single().PlanVersionId);
    }

    [Fact]
    public async Task AnotherHousehold_CannotReadOrChangeTheConversation()
    {
        await using var dbContext = CreateDbContext();
        var time = new FakeTimeProvider(Now);
        var firstId = await CreateHouseholdAsync(dbContext, "user_owner_a");
        var secondId = await CreateHouseholdAsync(dbContext, "user_owner_b");
        var firstPlanId = await SeedPlanAsync(dbContext, firstId);
        var first = CreateService(dbContext, Bind(firstId), time);
        var second = CreateService(dbContext, Bind(secondId), time);
        var started = await first.StartAsync();
        await first.AppendMessageAsync(started.Id, ConversationMessageRole.User, "My card.");
        await first.LinkPlanVersionAsync(started.Id, firstPlanId);

        Assert.Null(await second.GetOpenAsync());
        var missing = await Assert.ThrowsAsync<NotFoundException>(() => second.GetAsync(started.Id));
        var append = await Assert.ThrowsAsync<NotFoundException>(() =>
            second.AppendMessageAsync(started.Id, ConversationMessageRole.User, "Theirs"));
        var other = await second.StartAsync();
        var foreignPlan = await Assert.ThrowsAsync<NotFoundException>(() =>
            second.LinkPlanVersionAsync(other.Id, firstPlanId));

        Assert.Equal("That conversation was not found.", missing.Message);
        Assert.Equal("That conversation was not found.", append.Message);
        Assert.Equal("That saved plan was not found.", foreignPlan.Message);
        Assert.NotEqual(started.Id, other.Id);
        Assert.Empty(other.PlanVersions);
        Assert.Equal("My card.", (await first.GetAsync(started.Id)).Messages.Single().Text);
        Assert.Equal(2, await dbContext.Conversations.CountAsync(item =>
            item.Status == ConversationStatus.Open));
    }

    [Fact]
    public async Task DeleteHousehold_RemovesTheConversation()
    {
        await using var dbContext = CreateDbContext();
        var householdId = await CreateHouseholdAsync(dbContext, "user_owner");
        var planId = await SeedPlanAsync(dbContext, householdId);
        var service = CreateService(dbContext, Bind(householdId), new FakeTimeProvider(Now));
        var started = await service.StartAsync();
        await service.AppendMessageAsync(started.Id, ConversationMessageRole.User, "Hello");
        await service.AddQuestionAsync(started.Id, "Which bill?");
        await service.SetPreferenceAsync(started.Id, "Cash to keep", "One month");
        await service.LinkPlanVersionAsync(started.Id, planId);
        AssertConversationCascade(dbContext);

        var household = await dbContext.Households.SingleAsync(item => item.Id == householdId);
        dbContext.Households.Remove(household);
        await dbContext.SaveChangesAsync();

        Assert.Equal(0, await dbContext.Conversations.CountAsync());
        Assert.Equal(0, await dbContext.ConversationMessages.CountAsync());
        Assert.Equal(0, await dbContext.ConversationQuestions.CountAsync());
        Assert.Equal(0, await dbContext.ConversationPreferences.CountAsync());
        Assert.Equal(0, await dbContext.ConversationPlanLinks.CountAsync());
        Assert.Equal(0, await dbContext.PlanVersions.CountAsync());
    }

    private static void AssertConversationCascade(CarduiDBContext dbContext)
    {
        AssertCascade<Conversation>(dbContext, typeof(Household));
        AssertCascade<ConversationMessage>(dbContext, typeof(Conversation));
        AssertCascade<ConversationQuestion>(dbContext, typeof(Conversation));
        AssertCascade<ConversationPreference>(dbContext, typeof(Conversation));
        AssertCascade<ConversationPlanLink>(dbContext, typeof(Conversation));
        AssertCascade<ConversationPlanLink>(dbContext, typeof(PlanVersion));

        var conversation = dbContext.Model.FindEntityType(typeof(Conversation));
        Assert.NotNull(conversation);
        var open = conversation.GetIndexes().Single(index =>
            index.GetDatabaseName() == Conversation.OpenHouseholdIndexName);
        Assert.True(open.IsUnique);
        Assert.Equal("\"Status\" = 'Open'", open.GetFilter());
    }

    private static void AssertCascade<TDependent>(CarduiDBContext dbContext, Type principal)
    {
        var dependent = dbContext.Model.FindEntityType(typeof(TDependent));
        Assert.NotNull(dependent);
        var foreignKey = Assert.Single(
            dependent.GetForeignKeys(),
            key => key.PrincipalEntityType.ClrType == principal);
        Assert.Equal(DeleteBehavior.Cascade, foreignKey.DeleteBehavior);
    }

    private static async Task AssertClosed(Func<Task> action)
    {
        var error = await Assert.ThrowsAsync<BadRequestException>(action);
        Assert.Equal("This conversation is closed.", error.Message);
    }

    private static ConversationService CreateService(
        CarduiDBContext dbContext,
        HouseholdScope scope,
        FakeTimeProvider time)
    {
        return new ConversationService(dbContext, time, scope);
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

    private static async Task<Guid> SeedPlanAsync(CarduiDBContext dbContext, Guid householdId)
    {
        var id = Guid.NewGuid();
        dbContext.PlanVersions.Add(new PlanVersion
        {
            Id = id,
            HouseholdId = householdId,
            Number = 1,
            Status = PlanVersionStatus.Draft,
            CreatedAt = Now,
            CalculationVersion = "test",
            AsOf = new DateOnly(2026, 10, 16),
            PlanningCurrency = PlanningCurrencyRules.DefaultCode,
            Document = "{}"
        });
        await dbContext.SaveChangesAsync();
        return id;
    }

    private static CarduiDBContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CarduiDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new CarduiDBContext(options);
    }
}
