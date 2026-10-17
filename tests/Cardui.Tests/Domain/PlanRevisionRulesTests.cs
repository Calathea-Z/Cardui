using Cardui.Api.Domain.Planning;
using Cardui.Api.Domain.Plans;
using Cardui.Api.Domain.Recovery;
using Xunit;

namespace Cardui.Tests.Domain;

public class PlanRevisionRulesTests
{
    [Fact]
    public void Changes_ListsTheChoiceTheScenarioAndADroppedAction()
    {
        var accepted = DecisionPlanRules.Compose(Sample());
        var proposed = DecisionPlanRules.Compose(Sample() with
        {
            Kind = PayoffRolloverKind.ReclaimAll,
            Scenario = new DecisionPlanScenario(
                25m,
                2,
                500m,
                new DateOnly(2026, 12, 1),
                400m,
                1000m,
                PayoffPriorityChoice.MinimumRelease)
        });
        var shorter = proposed with { Actions = proposed.Actions.Take(1).ToList() };

        var changes = PlanRevisionRules.Changes(accepted, proposed);
        var dropped = PlanRevisionRules.Changes(proposed, shorter);

        Assert.Equal(PayoffRolloverKind.Rollover, One(changes, PlanRevisionField.FreedCash).BeforeKind);
        Assert.Equal(PayoffRolloverKind.ReclaimAll, One(changes, PlanRevisionField.FreedCash).AfterKind);
        Assert.Equal(0m, One(changes, PlanRevisionField.MonthlyExtra).BeforeAmount);
        Assert.Equal(25m, One(changes, PlanRevisionField.MonthlyExtra).AfterAmount);
        Assert.Equal(0, One(changes, PlanRevisionField.IncomeLoss).BeforeCount);
        Assert.Equal(2, One(changes, PlanRevisionField.IncomeLoss).AfterCount);
        Assert.Equal(0m, One(changes, PlanRevisionField.Windfall).BeforeAmount);
        Assert.Equal(500m, One(changes, PlanRevisionField.Windfall).AfterAmount);
        Assert.Null(One(changes, PlanRevisionField.WindfallDate).BeforeOn);
        Assert.Equal(new DateOnly(2026, 12, 1), One(changes, PlanRevisionField.WindfallDate).AfterOn);
        Assert.Null(One(changes, PlanRevisionField.FlexibleSpending).BeforeAmount);
        Assert.Equal(400m, One(changes, PlanRevisionField.FlexibleSpending).AfterAmount);
        Assert.Null(One(changes, PlanRevisionField.CashToKeep).BeforeAmount);
        Assert.Equal(1000m, One(changes, PlanRevisionField.CashToKeep).AfterAmount);
        Assert.Equal(
            PayoffPriorityChoice.Avalanche,
            One(changes, PlanRevisionField.PayoffPriority).BeforePriority);
        Assert.Equal(
            PayoffPriorityChoice.MinimumRelease,
            One(changes, PlanRevisionField.PayoffPriority).AfterPriority);
        Assert.DoesNotContain(changes, change => change.Field == PlanRevisionField.Reclaim);
        Assert.DoesNotContain(changes, change => change.Field == PlanRevisionField.Constraint);
        Assert.Contains(changes, change =>
            change.Field == PlanRevisionField.Action
            && change.AfterAction == DecisionPlanActionKind.HoldIncomeLoss);
        Assert.Contains(dropped, change =>
            change.Field == PlanRevisionField.Action
            && change.BeforeAction is not null
            && change.AfterAction is null);
    }

    [Fact]
    public void Changes_IsEmptyWhenTheDocumentMatches()
    {
        var plan = DecisionPlanRules.Compose(Sample());

        Assert.Empty(PlanRevisionRules.Changes(plan, plan));
        Assert.Equal(
            PlanRevisionRules.Fingerprint(plan),
            PlanRevisionRules.Fingerprint(DecisionPlanRules.Compose(Sample())));
    }

    [Fact]
    public void Changes_NamesCapturedCashAndAnyOtherDocumentDifference()
    {
        var accepted = DecisionPlanRules.Compose(Sample());
        var cash = accepted with { Facts = accepted.Facts with { StartingCash = 4200m } };
        var interest = accepted with { Facts = accepted.Facts with { TotalInterest = 250m } };

        var cashChange = PlanRevisionRules.Changes(accepted, cash);
        var interestChange = PlanRevisionRules.Changes(accepted, interest);

        Assert.Equal(5000m, One(cashChange, PlanRevisionField.StartingCash).BeforeAmount);
        Assert.Equal(4200m, One(cashChange, PlanRevisionField.StartingCash).AfterAmount);
        Assert.Equal(
            PlanRevisionField.CalculatedPlan,
            Assert.Single(interestChange).Field);
        Assert.Null(Assert.Single(interestChange).BeforeAmount);
        Assert.Null(Assert.Single(interestChange).AfterAmount);
    }

    [Fact]
    public void Changes_ShowsTheFirstActionsAndSkipsTheBaseline()
    {
        var proposed = DecisionPlanRules.Compose(Sample());

        var changes = PlanRevisionRules.Changes(null, proposed);

        Assert.DoesNotContain(changes, change => change.Field == PlanRevisionField.FreedCash);
        Assert.DoesNotContain(changes, change => change.Field == PlanRevisionField.MonthlyExtra);
        Assert.DoesNotContain(changes, change => change.Field == PlanRevisionField.PayoffPriority);
        Assert.Equal(
            [DecisionPlanActionKind.PayFirstDebt, DecisionPlanActionKind.UseFreedPayment],
            changes.Select(change => change.AfterAction));
        Assert.All(changes, change => Assert.Null(change.BeforeAction));
        Assert.Equal("Card", changes[0].AfterName);
        Assert.Equal(1, changes[0].Order);
    }

    [Fact]
    public void Stale_ChecksTheConversationThenTheDraftThenTheAcceptedPlanThenTheCalculation()
    {
        var current = Context();
        var otherConversation = current with { ConversationId = Guid.NewGuid() };
        var later = current with { ConversationUpdatedAtUtcTicks = current.ConversationUpdatedAtUtcTicks + 1 };
        var otherDraft = current with { OpenDraftId = Guid.NewGuid() };
        var otherAccepted = current with { AcceptedPlanId = Guid.NewGuid() };
        var otherPlan = current with { PlanFingerprint = new string('a', 64) };

        Assert.Null(PlanRevisionRules.Stale(current, current));
        Assert.Equal(
            PlanRevisionStaleReason.Conversation,
            PlanRevisionRules.Stale(later with { OpenDraftId = Guid.NewGuid() }, current));
        Assert.Equal(PlanRevisionStaleReason.Conversation, PlanRevisionRules.Stale(otherConversation, current));
        Assert.Equal(PlanRevisionStaleReason.Conversation, PlanRevisionRules.Stale(later, current));
        Assert.Equal(PlanRevisionStaleReason.Draft, PlanRevisionRules.Stale(otherDraft, current));
        Assert.Equal(PlanRevisionStaleReason.Accepted, PlanRevisionRules.Stale(otherAccepted, current));
        Assert.Equal(PlanRevisionStaleReason.Plan, PlanRevisionRules.Stale(otherPlan, current));
        Assert.Equal(
            "This conversation changed. Review the proposal again.",
            PlanRevisionRules.Message(PlanRevisionStaleReason.Conversation));
        Assert.Equal(
            "The plan draft changed outside this conversation. Review the proposal again.",
            PlanRevisionRules.Message(PlanRevisionStaleReason.Draft));
        Assert.Equal(
            "The accepted plan changed outside this conversation. Review the proposal again.",
            PlanRevisionRules.Message(PlanRevisionStaleReason.Accepted));
        Assert.Equal(
            "The plan changed outside this conversation. Review the proposal again.",
            PlanRevisionRules.Message(PlanRevisionStaleReason.Plan));
    }

    [Fact]
    public void Token_RoundTripsTheContextAndRejectsADamagedValue()
    {
        var context = Context();
        var token = PlanRevisionRules.Token(context);

        Assert.True(PlanRevisionRules.TryRead(token, out var read));
        Assert.Equal(context, read);
        Assert.False(PlanRevisionRules.TryRead(null, out _));
        Assert.False(PlanRevisionRules.TryRead("  ", out _));
        Assert.False(PlanRevisionRules.TryRead("v1|nope", out _));
        Assert.False(PlanRevisionRules.TryRead(token.ToUpperInvariant(), out _));
        Assert.False(PlanRevisionRules.TryRead(token + "|extra", out _));
        Assert.NotEqual(
            PlanRevisionRules.Fingerprint(DecisionPlanRules.Compose(Sample())),
            PlanRevisionRules.Fingerprint(DecisionPlanRules.Compose(Sample() with
            {
                Scenario = new DecisionPlanScenario(25m, 0, 0m, null, null, null)
            })));
    }

    #region Private Methods

    private static PlanRevisionChange One(
        IReadOnlyList<PlanRevisionChange> changes,
        PlanRevisionField field)
    {
        return Assert.Single(changes, change => change.Field == field);
    }

    private static PlanRevisionContext Context()
    {
        return new PlanRevisionContext(
            Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            638000000000000000,
            Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
            null,
            new string('b', 64));
    }

    /// <summary>
    /// A household that can carry out a one-debt plan: pay, bills, spending, a cash floor, and no shortfall.
    /// </summary>
    private static DecisionPlanInput Sample()
    {
        var cardId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        return new DecisionPlanInput(
            new DateOnly(2026, 10, 15),
            "USD",
            PayoffRolloverKind.Rollover,
            Facts(),
            new DecisionPlanScenario(0m, 0, 0m, null, null, null),
            [Debt(cardId, "Card", new DateOnly(2027, 4, 15))],
            [Step(cardId, "Card")],
            [],
            [],
            []);
    }

    /// <summary>
    /// Cash and budget figures for a plan that is not already short.
    /// </summary>
    private static DecisionPlanFacts Facts()
    {
        return new DecisionPlanFacts(
            5000m,
            1000m,
            4000m,
            1,
            new DateOnly(2026, 10, 14),
            0,
            true,
            true,
            800m,
            1000m,
            true,
            0,
            0,
            0,
            500m,
            false,
            new DecisionPlanGuardrail(false, false, false, null, 0m, false),
            null,
            null,
            null,
            50m,
            50m,
            new DateOnly(2027, 4, 15),
            100m);
    }

    /// <summary>
    /// One debt that the projection pays off.
    /// </summary>
    private static DecisionPlanDebt Debt(Guid id, string name, DateOnly paidOffOn)
    {
        return new DecisionPlanDebt(
            id,
            name,
            2000m,
            50m,
            19.99m,
            DebtScheduleStop.PaidOff,
            paidOffOn);
    }

    /// <summary>
    /// The card's payoff. The freed payment starts the month after it ends.
    /// </summary>
    private static DecisionPlanStep Step(Guid id, string name)
    {
        return new DecisionPlanStep(
            id,
            name,
            new DateOnly(2027, 3, 15),
            new DateOnly(2027, 4, 15),
            50m,
            50m);
    }

    #endregion
}
