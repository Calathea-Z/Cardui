using Cardui.Api.Data;
using Cardui.Api.Domain.Planning;
using Cardui.Api.Dtos.Planning;
using Cardui.Api.Exceptions;
using Cardui.Api.Models;
using Cardui.Api.Security;
using Cardui.Api.Services.Implementations;
using Cardui.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Cardui.Tests.Services;

public class PlanningCallsTests
{
    private const string Token = "access-sandbox-do-not-send";

    private const string DraftDocument = """{"kept":"draft"}""";

    private const string AcceptedDocument = """{"kept":"accepted"}""";

    private static readonly DateTimeOffset Now = new(2026, 10, 16, 18, 0, 0, TimeSpan.Zero);

    private static readonly DateOnly Today = new(2026, 10, 16);

    [Fact]
    public async Task Failure_LeavesTheDraftAndSaysToContinue()
    {
        await using var db = CreateDbContext();
        var household = await SeedAsync(db);
        var draft = await AddVersionAsync(db, household.HouseholdId, 1, PlanVersionStatus.Draft, DraftDocument);
        var accepted = await AddVersionAsync(
            db,
            household.HouseholdId,
            2,
            PlanVersionStatus.Accepted,
            AcceptedDocument);
        var conversation = await household.Conversations.StartAsync();
        await household.Conversations.AppendMessageAsync(
            conversation.Id,
            ConversationMessageRole.User,
            "What is next?");
        var saved = await household.Conversations.GetAsync(conversation.Id);
        household.Time.Advance(TimeSpan.FromMinutes(5));
        var model = new ScriptedModel
        {
            Error = new PlanningModelException("The planning model could not answer. Please try again.")
        };

        var result = await household.Calls(model).AskAsync(
            conversation.Id,
            new PlanningModelRequest("What is next?", "Stay brief."));

        Assert.Equal(PlanningCallResult.ContinueManually, result.Result);
        Assert.Null(result.Text);
        Assert.Equal(PlanningCallRules.UnavailableNotice, result.Notice);
        Assert.Equal(draft.Id, result.DraftId);
        Assert.Equal(1, model.Calls);
        Assert.Contains(
            "Reason NoAnswer.",
            string.Join(" ", household.Logger.Messages),
            StringComparison.Ordinal);
        Assert.DoesNotContain("What is next?", string.Join(" ", household.Logger.Messages), StringComparison.Ordinal);
        Assert.DoesNotContain("draft", string.Join(" ", household.Logger.Messages), StringComparison.Ordinal);
        await AssertUnchangedAsync(db, household, saved.UpdatedAt, draft.Id, accepted.Id);
    }

    [Fact]
    public async Task Answer_ReturnsTheTextAndLeavesTheDraft()
    {
        await using var db = CreateDbContext();
        var household = await SeedAsync(db);
        var draft = await AddVersionAsync(db, household.HouseholdId, 1, PlanVersionStatus.Draft, DraftDocument);
        var conversation = await household.Conversations.StartAsync();
        await household.Conversations.AppendMessageAsync(
            conversation.Id,
            ConversationMessageRole.User,
            "What is next?");
        var saved = await household.Conversations.GetAsync(conversation.Id);
        var model = new ScriptedModel();

        var result = await household.Calls(model).AskAsync(
            conversation.Id,
            new PlanningModelRequest("What is next?", null));

        Assert.Equal(PlanningCallResult.Answered, result.Result);
        Assert.Equal("Keep the minimum.", result.Text);
        Assert.Null(result.Notice);
        Assert.Equal(draft.Id, result.DraftId);
        Assert.Equal(1, model.Calls);
        Assert.Empty(household.Logger.Messages);
        var after = await household.Conversations.GetAsync(conversation.Id);
        Assert.Equal(saved.UpdatedAt, after.UpdatedAt);
        Assert.Equal("What is next?", Assert.Single(after.Messages).Text);
        Assert.Equal(DraftDocument, (await db.PlanVersions.SingleAsync(version => version.Id == draft.Id)).Document);
        Assert.Equal(0, await db.PlanVersions.CountAsync(version => version.Status == PlanVersionStatus.Accepted));
    }

    [Fact]
    public async Task LimitAndMissingModel_LeaveTheDraft()
    {
        await using var db = CreateDbContext();
        var household = await SeedAsync(db);
        var draft = await AddVersionAsync(db, household.HouseholdId, 1, PlanVersionStatus.Draft, DraftDocument);
        var conversation = await household.Conversations.StartAsync();
        var tooLong = new string('a', PlanningCallRules.MaxInputCharacters + 1);
        var blocked = new ScriptedModel();
        var missing = new ScriptedModel { Error = new PlanningModelNotConfiguredException() };

        var limit = await household.Calls(blocked).AskAsync(
            conversation.Id,
            new PlanningModelRequest(tooLong, "Stay brief."));
        var unavailable = await household.Calls(missing).AskAsync(
            conversation.Id,
            new PlanningModelRequest("What is next?", null));

        Assert.Equal(PlanningCallResult.ContinueManually, limit.Result);
        Assert.Equal(PlanningCallRules.LimitNotice, limit.Notice);
        Assert.Equal(draft.Id, limit.DraftId);
        Assert.Equal(0, blocked.Calls);
        Assert.Equal(PlanningCallResult.ContinueManually, unavailable.Result);
        Assert.Equal(PlanningCallRules.NotConfiguredNotice, unavailable.Notice);
        Assert.Equal(draft.Id, unavailable.DraftId);
        Assert.Equal(1, missing.Calls);
        var logged = string.Join(" ", household.Logger.Messages);
        Assert.Contains("Reason CharacterCap.", logged, StringComparison.Ordinal);
        Assert.Contains("Reason NotConfigured.", logged, StringComparison.Ordinal);
        Assert.DoesNotContain(tooLong, logged, StringComparison.Ordinal);
        Assert.Equal(DraftDocument, (await db.PlanVersions.SingleAsync(version => version.Id == draft.Id)).Document);
        Assert.Equal(0, await db.ConversationMessages.CountAsync());
    }

    [Fact]
    public async Task ClosedTokenAndOtherHousehold_DoNotChangeTheDraft()
    {
        await using var db = CreateDbContext();
        var household = await SeedAsync(db);
        var draft = await AddVersionAsync(db, household.HouseholdId, 1, PlanVersionStatus.Draft, DraftDocument);
        var conversation = await household.Conversations.StartAsync();
        await household.Conversations.CloseAsync(conversation.Id);
        var model = new ScriptedModel();
        var calls = household.Calls(model);

        var closed = await Assert.ThrowsAsync<BadRequestException>(() =>
            calls.AskAsync(conversation.Id, new PlanningModelRequest("What is next?", null)));
        var restarted = await household.Conversations.StartAsync();
        var token = await Assert.ThrowsAsync<BadRequestException>(() =>
            calls.AskAsync(
                restarted.Id,
                new PlanningModelRequest("See " + Token, "Stay brief.")));
        Assert.Equal(0, model.Calls);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            calls.AskAsync(
                restarted.Id,
                new PlanningModelRequest("What is next?", null),
                cancellation.Token));
        household.Scope.Bind(household.OtherHouseholdId);
        var foreign = await Assert.ThrowsAsync<NotFoundException>(() =>
            calls.AskAsync(restarted.Id, new PlanningModelRequest("What is next?", null)));
        var missing = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateCalls(db, new HouseholdScope(), household.Time, new ScriptedModel(), new ListLogger())
                .AskAsync(restarted.Id, new PlanningModelRequest("What is next?", null)));

        Assert.Equal("This conversation is closed.", closed.Message);
        Assert.Equal(PlanningShareRules.TokenRefusal, token.Message);
        Assert.Equal("That conversation was not found.", foreign.Message);
        Assert.Equal("A household scope is required.", missing.Message);
        Assert.True(model.Calls <= 1);
        Assert.DoesNotContain(
            household.Logger.Messages,
            message => message.Contains(Token, StringComparison.Ordinal));
        Assert.Equal(PlanVersionStatus.Draft, (await db.PlanVersions.SingleAsync(version => version.Id == draft.Id)).Status);
        Assert.Equal(DraftDocument, (await db.PlanVersions.SingleAsync(version => version.Id == draft.Id)).Document);
        Assert.Equal(0, await db.ConversationMessages.CountAsync());
    }

    #region Private Methods

    /// <summary>
    /// The draft, the accepted plan, and the stored turn are the ones saved before the call.
    /// </summary>
    private static async Task AssertUnchangedAsync(
        CarduiDBContext db,
        HouseholdHarness household,
        DateTimeOffset updatedAt,
        Guid draftId,
        Guid acceptedId)
    {
        var after = await household.Conversations.GetAsync(
            (await db.Conversations.SingleAsync(conversation => conversation.HouseholdId == household.HouseholdId
                && conversation.Status == ConversationStatus.Open)).Id);
        Assert.Equal(updatedAt, after.UpdatedAt);
        Assert.Equal("What is next?", Assert.Single(after.Messages).Text);
        Assert.Equal(ConversationMessageRole.User, Assert.Single(after.Messages).Role);
        var draft = await db.PlanVersions.SingleAsync(version => version.Id == draftId);
        var accepted = await db.PlanVersions.SingleAsync(version => version.Id == acceptedId);
        Assert.Equal(PlanVersionStatus.Draft, draft.Status);
        Assert.Equal(DraftDocument, draft.Document);
        Assert.Equal(PlanVersionStatus.Accepted, accepted.Status);
        Assert.Equal(AcceptedDocument, accepted.Document);
        Assert.Equal(2, await db.PlanVersions.CountAsync());
        Assert.Equal(1, await db.ConversationMessages.CountAsync());
    }

    private static async Task<PlanVersion> AddVersionAsync(
        CarduiDBContext db,
        Guid householdId,
        int number,
        PlanVersionStatus status,
        string document)
    {
        var version = new PlanVersion
        {
            Id = Guid.NewGuid(),
            HouseholdId = householdId,
            Number = number,
            Status = status,
            CreatedAt = Now,
            CalculationVersion = "test",
            AsOf = Today,
            PlanningCurrency = "USD",
            Document = document
        };
        db.PlanVersions.Add(version);
        await db.SaveChangesAsync();
        return version;
    }

    private static async Task<HouseholdHarness> SeedAsync(CarduiDBContext db)
    {
        var time = new FakeTimeProvider(Now);
        var households = new HouseholdsService(db, time);
        var home = await households.GetOrCreateForOwnerAsync("owner_home");
        var other = await households.GetOrCreateForOwnerAsync("owner_other");
        var scope = new HouseholdScope();
        scope.Bind(home.Id);
        return new HouseholdHarness(
            db,
            new ConversationService(db, time, scope),
            time,
            scope,
            new ListLogger(),
            home.Id,
            other.Id);
    }

    private static PlanningCalls CreateCalls(
        CarduiDBContext db,
        HouseholdScope scope,
        TimeProvider time,
        IPlanningModel model,
        ILogger<PlanningCalls> logger)
    {
        return new PlanningCalls(
            new ConversationService(db, time, scope),
            model,
            db,
            scope,
            logger);
    }

    private static CarduiDBContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CarduiDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new CarduiDBContext(options);
    }

    private sealed record HouseholdHarness(
        CarduiDBContext Db,
        ConversationService Conversations,
        FakeTimeProvider Time,
        HouseholdScope Scope,
        ListLogger Logger,
        Guid HouseholdId,
        Guid OtherHouseholdId)
    {
        /// <summary>
        /// Builds the call service around one model. The logger is the harness logger.
        /// </summary>
        public PlanningCalls Calls(IPlanningModel model)
        {
            return new PlanningCalls(Conversations, model, Db, Scope, Logger);
        }
    }

    /// <summary>
    /// Returns one canned reply, or throws the scripted failure.
    /// </summary>
    private sealed class ScriptedModel : IPlanningModel
    {
        public int Calls { get; private set; }

        public Exception? Error { get; init; }

        public string Text { get; init; } = "Keep the minimum.";

        /// <inheritdoc />
        public Task<PlanningModelReply> ReplyAsync(
            PlanningModelRequest request,
            CancellationToken cancellationToken)
        {
            Calls++;
            cancellationToken.ThrowIfCancellationRequested();
            if (Error is not null)
            {
                throw Error;
            }

            return Task.FromResult(new PlanningModelReply(Text));
        }
    }

    /// <summary>
    /// Keeps formatted log lines so a test can check that a draft was not written.
    /// </summary>
    private sealed class ListLogger : ILogger<PlanningCalls>
    {
        public List<string> Messages { get; } = [];

        /// <inheritdoc />
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return null;
        }

        /// <inheritdoc />
        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        /// <inheritdoc />
        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }
    }

    #endregion
}
