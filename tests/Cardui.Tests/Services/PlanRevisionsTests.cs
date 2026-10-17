using Cardui.Api.Data;
using Cardui.Api.Domain.Planning;
using Cardui.Api.Domain.Recovery;
using Cardui.Api.Dtos.Income;
using Cardui.Api.Dtos.Plan;
using Cardui.Api.Dtos.Planning;
using Cardui.Api.Exceptions;
using Cardui.Api.Models;
using Cardui.Api.Security;
using Cardui.Api.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Cardui.Tests.Services;

public class PlanRevisionsTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 10, 16, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Review_ShowsTheProposedExtraAndWritesNothing()
    {
        await using var db = CreateDbContext();
        var household = await SeedAsync(db);

        var review = await household.Revisions.ReviewAsync(
            household.ConversationId,
            Extra(100m),
            PayoffRolloverKind.Rollover);
        var again = await household.Revisions.ReviewAsync(
            household.ConversationId,
            Extra(100m),
            PayoffRolloverKind.Rollover,
            review.ReviewToken);

        Assert.False(review.Applied);
        Assert.False(review.Stale);
        Assert.Null(review.Reason);
        Assert.False(review.ReplacesOpenDraft);
        Assert.Null(review.OpenDraftId);
        Assert.Null(review.AcceptedPlanId);
        Assert.Equal(100m, One(review, PlanRevisionField.MonthlyExtra).AfterAmount);
        Assert.Null(One(review, PlanRevisionField.MonthlyExtra).BeforeAmount);
        Assert.Contains(review.Changes, change => change.Field == PlanRevisionField.Action);
        Assert.False(again.Stale);
        Assert.Equal(review.ReviewToken, again.ReviewToken);
        Assert.Equal(0, await db.PlanVersions.CountAsync());
        Assert.Equal(0, await db.ConversationMessages.CountAsync());
        Assert.Equal(household.UpdatedAt, (await household.Conversations.GetAsync(household.ConversationId)).UpdatedAt);
    }

    [Fact]
    public async Task Review_NamesADraftThatWouldBeReplaced()
    {
        await using var db = CreateDbContext();
        var household = await SeedAsync(db);
        var draft = await household.Plans.SaveDraftAsync(Save(10m, null));

        var review = await household.Revisions.ReviewAsync(
            household.ConversationId,
            Extra(40m),
            PayoffRolloverKind.Rollover);

        Assert.False(review.Applied);
        Assert.True(review.ReplacesOpenDraft);
        Assert.Equal(draft.Id, review.OpenDraftId);
        Assert.Equal(40m, One(review, PlanRevisionField.MonthlyExtra).AfterAmount);
        Assert.Equal(PlanVersionStatus.Draft, (await household.Plans.GetAsync(draft.Id)).Status);
        Assert.Equal(1, await db.PlanVersions.CountAsync());
        Assert.Equal(household.UpdatedAt, (await household.Conversations.GetAsync(household.ConversationId)).UpdatedAt);
    }

    [Fact]
    public async Task Accept_KeepsTheReviewedPlanAndARepeatDoesNotAddAnother()
    {
        await using var db = CreateDbContext();
        var household = await SeedAsync(db);
        var review = await household.Revisions.ReviewAsync(
            household.ConversationId,
            Extra(0m),
            PayoffRolloverKind.Rollover);

        var accepted = await household.Revisions.AcceptAsync(
            household.ConversationId,
            Extra(0m),
            PayoffRolloverKind.Rollover,
            review.ReviewToken!);
        var again = await household.Revisions.AcceptAsync(
            household.ConversationId,
            Extra(0m),
            PayoffRolloverKind.Rollover,
            review.ReviewToken!);
        var current = await household.Revisions.ReviewAsync(
            household.ConversationId,
            Extra(0m),
            PayoffRolloverKind.Rollover);

        Assert.Equal(accepted.Id, again.Id);
        Assert.Equal(PlanVersionStatus.Accepted, again.Status);
        Assert.Equal(0m, again.Plan.Scenario.MonthlyExtra);
        Assert.Empty(current.Changes);
        Assert.Equal(accepted.Id, current.AcceptedPlanId);
        Assert.False(current.ReplacesOpenDraft);
        Assert.Equal(1, await db.PlanVersions.CountAsync(version =>
            version.Status == PlanVersionStatus.Accepted));
        Assert.Equal(0, await db.PlanVersions.CountAsync(version =>
            version.Status == PlanVersionStatus.Draft));
        Assert.Equal(0, await db.ConversationMessages.CountAsync());
        Assert.Equal(household.UpdatedAt, (await household.Conversations.GetAsync(household.ConversationId)).UpdatedAt);
    }

    [Fact]
    public async Task Accept_RejectsAMovedConversationEvenWhenThePlanAlreadyMatches()
    {
        await using var db = CreateDbContext();
        var household = await SeedAsync(db);
        var review = await household.Revisions.ReviewAsync(
            household.ConversationId,
            Extra(0m),
            PayoffRolloverKind.Rollover);
        var accepted = await household.Revisions.AcceptAsync(
            household.ConversationId,
            Extra(0m),
            PayoffRolloverKind.Rollover,
            review.ReviewToken!);
        household.Time.Advance(TimeSpan.FromMinutes(1));
        await household.Conversations.AppendMessageAsync(
            household.ConversationId,
            ConversationMessageRole.User,
            "Keep the same plan.");

        var error = await Assert.ThrowsAsync<ConflictException>(() =>
            household.Revisions.AcceptAsync(
                household.ConversationId,
                Extra(0m),
                PayoffRolloverKind.Rollover,
                review.ReviewToken!));

        Assert.Equal(
            "This conversation changed. Review the proposal again.",
            error.Message);
        Assert.Equal(PlanVersionStatus.Accepted, (await household.Plans.GetAsync(accepted.Id)).Status);
        Assert.Equal(1, await db.PlanVersions.CountAsync());
        Assert.Equal(1, await db.ConversationMessages.CountAsync());
    }

    [Fact]
    public async Task Accept_RejectsADraftSavedOutsideChat()
    {
        await using var db = CreateDbContext();
        var household = await SeedAsync(db);
        var review = await household.Revisions.ReviewAsync(
            household.ConversationId,
            Extra(40m),
            PayoffRolloverKind.Rollover);
        var draft = await household.Plans.SaveDraftAsync(Save(10m, null));

        var error = await Assert.ThrowsAsync<ConflictException>(() =>
            household.Revisions.AcceptAsync(
                household.ConversationId,
                Extra(40m),
                PayoffRolloverKind.Rollover,
                review.ReviewToken!));

        Assert.Equal(
            "The plan draft changed outside this conversation. Review the proposal again.",
            error.Message);
        Assert.Equal(PlanVersionStatus.Draft, (await household.Plans.GetAsync(draft.Id)).Status);
        Assert.Equal(10m, (await household.Plans.GetAsync(draft.Id)).Plan.Scenario.MonthlyExtra);
        Assert.Equal(0, await db.PlanVersions.CountAsync(version =>
            version.Status == PlanVersionStatus.Accepted));
    }

    [Fact]
    public async Task Accept_RejectsAFactEditedOutsideChat()
    {
        await using var db = CreateDbContext();
        var household = await SeedAsync(db);
        var review = await household.Revisions.ReviewAsync(
            household.ConversationId,
            Extra(0m),
            PayoffRolloverKind.Rollover);
        await household.Incomes.CreateAsync(new UpsertIncomeSourceDto
        {
            Name = "Paycheck",
            TakeHomeAmount = 2000m,
            Cadence = IncomeCadence.Monthly,
            NextPaymentDate = new DateOnly(2026, 10, 20),
            Reliability = IncomeReliability.Steady
        });

        var error = await Assert.ThrowsAsync<ConflictException>(() =>
            household.Revisions.AcceptAsync(
                household.ConversationId,
                Extra(0m),
                PayoffRolloverKind.Rollover,
                review.ReviewToken!));

        Assert.Equal(
            "The plan changed outside this conversation. Review the proposal again.",
            error.Message);
        Assert.Equal(0, await db.PlanVersions.CountAsync());
        Assert.Equal(1, await db.IncomeSources.CountAsync());
        Assert.Equal(household.UpdatedAt, (await household.Conversations.GetAsync(household.ConversationId)).UpdatedAt);
    }

    [Fact]
    public async Task Accept_RejectsAConversationThatMoved()
    {
        await using var db = CreateDbContext();
        var household = await SeedAsync(db);
        var review = await household.Revisions.ReviewAsync(
            household.ConversationId,
            Extra(0m),
            PayoffRolloverKind.Rollover);
        household.Time.Advance(TimeSpan.FromMinutes(1));
        await household.Conversations.AppendMessageAsync(
            household.ConversationId,
            ConversationMessageRole.User,
            "The rent date changed.");

        var stale = await household.Revisions.ReviewAsync(
            household.ConversationId,
            Extra(0m),
            PayoffRolloverKind.Rollover,
            review.ReviewToken);
        var error = await Assert.ThrowsAsync<ConflictException>(() =>
            household.Revisions.AcceptAsync(
                household.ConversationId,
                Extra(0m),
                PayoffRolloverKind.Rollover,
                review.ReviewToken!));

        Assert.True(stale.Stale);
        Assert.Equal(PlanRevisionStaleReason.Conversation, stale.Reason);
        Assert.Equal(
            "This conversation changed. Review the proposal again.",
            stale.Message);
        Assert.Null(stale.ReviewToken);
        Assert.Empty(stale.Changes);
        Assert.False(stale.Applied);
        Assert.Equal(error.Message, stale.Message);
        Assert.Equal(0, await db.PlanVersions.CountAsync());
        Assert.Equal(1, await db.ConversationMessages.CountAsync());
    }

    [Fact]
    public async Task Accept_RejectsAnAcceptedPlanThatChangedOutsideChat()
    {
        await using var db = CreateDbContext();
        var household = await SeedAsync(db);
        var first = await household.Revisions.ReviewAsync(
            household.ConversationId,
            Extra(0m),
            PayoffRolloverKind.Rollover);
        await household.Revisions.AcceptAsync(
            household.ConversationId,
            Extra(0m),
            PayoffRolloverKind.Rollover,
            first.ReviewToken!);
        var review = await household.Revisions.ReviewAsync(
            household.ConversationId,
            Extra(80m),
            PayoffRolloverKind.Rollover);
        var outside = await household.Plans.SaveDraftAsync(Save(20m, null));
        await household.Plans.AcceptAsync(outside.Id);

        var error = await Assert.ThrowsAsync<ConflictException>(() =>
            household.Revisions.AcceptAsync(
                household.ConversationId,
                Extra(80m),
                PayoffRolloverKind.Rollover,
                review.ReviewToken!));
        var kept = await household.Plans.ListAsync();

        Assert.Equal(
            "The accepted plan changed outside this conversation. Review the proposal again.",
            error.Message);
        Assert.Equal(20m, kept.Single(version => version.Status == PlanVersionStatus.Accepted).Plan.Scenario.MonthlyExtra);
        Assert.Equal(0, kept.Count(version => version.Plan.Scenario.MonthlyExtra == 80m));
        Assert.Equal(80m, One(review, PlanRevisionField.MonthlyExtra).AfterAmount);
        Assert.Equal(0m, One(review, PlanRevisionField.MonthlyExtra).BeforeAmount);
    }

    [Fact]
    public async Task Review_RejectsAClosedConversationAnotherHouseholdAndAMissingHousehold()
    {
        await using var db = CreateDbContext();
        var household = await SeedAsync(db);
        var other = await new HouseholdsService(db, household.Time)
            .GetOrCreateForOwnerAsync("owner_other");
        await household.Conversations.CloseAsync(household.ConversationId);

        var closed = await Assert.ThrowsAsync<BadRequestException>(() =>
            household.Revisions.ReviewAsync(
                household.ConversationId,
                Extra(0m),
                PayoffRolloverKind.Rollover));
        household.Scope.Bind(other.Id);
        var foreign = await Assert.ThrowsAsync<NotFoundException>(() =>
            household.Revisions.ReviewAsync(
                household.ConversationId,
                Extra(0m),
                PayoffRolloverKind.Rollover));
        var missing = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateRevisions(db, new HouseholdScope(), household.Time).ReviewAsync(
                Guid.NewGuid(),
                Extra(0m),
                PayoffRolloverKind.Rollover));

        Assert.Equal("This conversation is closed.", closed.Message);
        Assert.Equal("That conversation was not found.", foreign.Message);
        Assert.Equal("A household scope is required.", missing.Message);
        Assert.Equal(0, await db.PlanVersions.CountAsync());
    }

    [Fact]
    public async Task Accept_RequiresAReadableReview()
    {
        await using var db = CreateDbContext();
        var household = await SeedAsync(db);

        var blank = await Assert.ThrowsAsync<BadRequestException>(() =>
            household.Revisions.AcceptAsync(
                household.ConversationId,
                Extra(0m),
                PayoffRolloverKind.Rollover,
                "  "));
        var damaged = await Assert.ThrowsAsync<BadRequestException>(() =>
            household.Revisions.AcceptAsync(
                household.ConversationId,
                Extra(0m),
                PayoffRolloverKind.Rollover,
                "v1|nope"));
        var kind = await Assert.ThrowsAsync<BadRequestException>(() =>
            household.Revisions.ReviewAsync(
                household.ConversationId,
                Extra(0m),
                PayoffRolloverKind.Reclaim));
        var months = await Assert.ThrowsAsync<BadRequestException>(() =>
            household.Revisions.ReviewAsync(
                household.ConversationId,
                new PlanScenarioQuery { IncomeLossMonths = 99 },
                PayoffRolloverKind.Rollover));

        Assert.Equal("Review the proposal before accepting it.", blank.Message);
        Assert.Equal("This review could not be read. Review the proposal again.", damaged.Message);
        Assert.Equal("Enter how much freed cash to keep each month.", kind.Message);
        Assert.Equal("Income loss has to be from 0 to 18 months.", months.Message);
        Assert.Equal(0, await db.PlanVersions.CountAsync());
        Assert.Equal(0, await db.ConversationMessages.CountAsync());
    }

    #region Private Methods

    private static PlanRevisionChangeDto One(PlanRevisionReviewDto review, PlanRevisionField field)
    {
        return Assert.Single(review.Changes, change => change.Field == field);
    }

    private static PlanScenarioQuery Extra(decimal amount)
    {
        return new PlanScenarioQuery { MonthlyExtra = amount };
    }

    private static SavePlanVersionDto Save(decimal extra, Guid? replacesDraftId)
    {
        return new SavePlanVersionDto
        {
            Kind = PayoffRolloverKind.Rollover,
            MonthlyExtra = extra,
            ReplacesDraftId = replacesDraftId
        };
    }

    private static async Task<HouseholdHarness> SeedAsync(CarduiDBContext db)
    {
        var time = new FakeTimeProvider(Now);
        var home = await new HouseholdsService(db, time).GetOrCreateForOwnerAsync("owner_home");
        var scope = new HouseholdScope();
        scope.Bind(home.Id);
        var conversations = new ConversationService(db, time, scope);
        var started = await conversations.StartAsync();
        var (plan, saved) = CreatePlans(db, scope, time);
        return new HouseholdHarness(
            new PlanRevisions(db, plan, saved, scope),
            saved,
            conversations,
            new IncomeSourcesService(db, time, scope),
            started.Id,
            started.UpdatedAt,
            time,
            scope);
    }

    private static PlanRevisions CreateRevisions(
        CarduiDBContext db,
        HouseholdScope scope,
        TimeProvider time)
    {
        var (plan, saved) = CreatePlans(db, scope, time);
        return new PlanRevisions(db, plan, saved, scope);
    }

    private static (PlanService Plan, SavedPlanService Saved) CreatePlans(
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
        return (plan, new SavedPlanService(db, plan, time, scope));
    }

    private static CarduiDBContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CarduiDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings =>
                warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new CarduiDBContext(options);
    }

    private sealed record HouseholdHarness(
        PlanRevisions Revisions,
        SavedPlanService Plans,
        ConversationService Conversations,
        IncomeSourcesService Incomes,
        Guid ConversationId,
        DateTimeOffset UpdatedAt,
        FakeTimeProvider Time,
        HouseholdScope Scope);

    #endregion
}
