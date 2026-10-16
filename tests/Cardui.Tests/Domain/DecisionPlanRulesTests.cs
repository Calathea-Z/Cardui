using System.Text.Json.Nodes;
using Cardui.Api.Domain.Plans;
using Cardui.Api.Domain.Recovery;
using Xunit;

namespace Cardui.Tests.Domain;

public class DecisionPlanRulesTests
{
    [Fact]
    public void Compose_PutsTheShortfallAndMissingFactsAheadOfTheNextPayment()
    {
        var plan = DecisionPlanRules.Compose(Sample() with
        {
            Debts = [],
            Steps = [],
            MissingBalanceNames = ["Store card"],
            Facts = Sample().Facts with
            {
                HasIncome = false,
                ShortfallOn = new DateOnly(2026, 11, 3)
            }
        });

        Assert.Equal(
            [
                DecisionPlanActionKind.CoverCashShortfall,
                DecisionPlanActionKind.AddIncome,
                DecisionPlanActionKind.FinishDebtTerms
            ],
            plan.Actions.Select(action => action.Kind));
        Assert.Equal([1, 2, 3], plan.Actions.Select(action => action.Order));
        Assert.Equal("Store card", plan.Actions[2].Name);
        Assert.Equal(1, plan.Actions[2].Count);
        Assert.DoesNotContain(plan.Actions, action => action.Kind == DecisionPlanActionKind.PayFirstDebt);
        Assert.Contains(plan.Milestones, milestone =>
            milestone.Kind == DecisionPlanMilestoneKind.CashShortfall
            && milestone.On == new DateOnly(2026, 11, 3));
        Assert.Contains(plan.Risks, risk => risk.Kind == DecisionPlanDetailKind.PayoffConditional);
        Assert.Contains(plan.Missing, gap =>
            gap.Kind == DecisionPlanDetailKind.MissingBalance && gap.Name == "Store card");
    }

    [Fact]
    public void Compose_PaysTheFirstDebtWhenThePlanCanBeCarriedOut()
    {
        var plan = DecisionPlanRules.Compose(Sample());

        Assert.Equal(
            [DecisionPlanActionKind.PayFirstDebt, DecisionPlanActionKind.UseFreedPayment],
            plan.Actions.Select(action => action.Kind));
        Assert.Equal("Card", plan.Actions[0].Name);
        Assert.Equal(50m, plan.Actions[0].Amount);
        Assert.Null(plan.Actions[1].NextName);
        Assert.Equal(new DateOnly(2027, 4, 15), plan.Actions[1].On);
        Assert.DoesNotContain(plan.Milestones, milestone =>
            milestone.Kind == DecisionPlanMilestoneKind.CashShortfall);
        Assert.Contains(plan.Milestones, milestone =>
            milestone.Kind == DecisionPlanMilestoneKind.FirstPayoff
            && milestone.On == new DateOnly(2027, 3, 15));
        Assert.DoesNotContain(plan.Risks, risk => risk.Kind == DecisionPlanDetailKind.PayoffConditional);
        Assert.Contains(plan.ReviewConditions, review => review.Kind == DecisionPlanDetailKind.FactsChange);
        Assert.DoesNotContain(
            plan.ReviewConditions,
            review => review.Kind == DecisionPlanDetailKind.EmergencyChoice);
    }

    [Fact]
    public void Compose_SendsAFreedPaymentToTheNextDebtOnlyWhenPaymentsRollForward()
    {
        var rolled = DecisionPlanRules.Compose(TwoDebts(PayoffRolloverKind.Rollover));
        var reclaimed = DecisionPlanRules.Compose(TwoDebts(PayoffRolloverKind.ReclaimAll));

        Assert.Equal("Loan", rolled.Actions.Single(action =>
            action.Kind == DecisionPlanActionKind.UseFreedPayment).NextName);
        Assert.Null(reclaimed.Actions.Single(action =>
            action.Kind == DecisionPlanActionKind.UseFreedPayment).NextName);
    }

    [Fact]
    public void Compose_PlansTheIncomeGapAndWindfallBeforeTheNextPayment()
    {
        var plan = DecisionPlanRules.Compose(Sample() with
        {
            Scenario = new DecisionPlanScenario(
                0m,
                2,
                500m,
                new DateOnly(2026, 12, 1),
                null,
                null)
        });

        Assert.Equal(
            [
                DecisionPlanActionKind.HoldIncomeLoss,
                DecisionPlanActionKind.HoldWindfall,
                DecisionPlanActionKind.PayFirstDebt
            ],
            plan.Actions.Select(action => action.Kind));
        Assert.Equal(500m, plan.Actions[1].Amount);
        Assert.Contains(plan.ReviewConditions, review =>
            review.Kind == DecisionPlanDetailKind.IncomeLossEnds
            && review.On == new DateOnly(2026, 12, 15));
        Assert.Contains(plan.Assumptions, assumption =>
            assumption.Kind == DecisionPlanDetailKind.ScenarioOrder);
        Assert.Contains(plan.Risks, risk => risk.Kind == DecisionPlanDetailKind.WindfallNotApplied);
    }

    [Fact]
    public void Compose_LowersTheExtraWhenItCreatesAShortfallAndNamesBothGaps()
    {
        var facts = Sample().Facts with
        {
            ShortfallOn = new DateOnly(2026, 11, 2),
            Guardrail = new DecisionPlanGuardrail(
                true,
                true,
                true,
                new DateOnly(2026, 10, 28),
                40m,
                true)
        };
        var plan = DecisionPlanRules.Compose(Sample() with
        {
            Facts = facts,
            Scenario = Sample().Scenario with { MonthlyExtra = 200m }
        });

        Assert.Equal(DecisionPlanActionKind.CoverCashShortfall, plan.Actions[0].Kind);
        Assert.Equal(DecisionPlanActionKind.LowerExtra, plan.Actions[1].Kind);
        Assert.Equal(3, plan.Actions[1].Count);
        Assert.True(plan.Actions[1].AlreadyShortWithoutExtra);
        Assert.Equal(40m, plan.Actions[1].Amount);
        Assert.Equal(DecisionPlanActionKind.PayFirstDebt, plan.Actions[2].Kind);
    }

    [Fact]
    public void Compose_AsksForADebtWhenNothingElseBlocksThePlan()
    {
        var plan = DecisionPlanRules.Compose(Sample() with
        {
            Debts = [],
            Steps = [],
            Facts = Sample().Facts with
            {
                RecurringRoom = 0m,
                PaidOffOn = null,
                TotalInterest = 0m,
                StartingObligation = null
            }
        });

        Assert.Equal([DecisionPlanActionKind.AddDebt], plan.Actions.Select(action => action.Kind));
    }

    [Fact]
    public void Compose_ReturnsTheSamePlanForTheSameInput()
    {
        var first = DecisionPlanRules.Compose(Sample());
        var second = DecisionPlanRules.Compose(Sample());
        var changed = DecisionPlanRules.Compose(Sample() with
        {
            Facts = Sample().Facts with { HasBills = false }
        });

        Assert.True(DecisionPlanRules.Same(first, second));
        Assert.False(DecisionPlanRules.Same(first, changed));
        Assert.Equal(DecisionPlanRules.CalculationVersion, first.CalculationVersion);

        var stored = DecisionPlanJson.Serialize(first);
        var read = DecisionPlanJson.Deserialize(stored);
        Assert.True(DecisionPlanRules.Same(first, read));
        Assert.Equal(stored, DecisionPlanJson.Serialize(read));
    }

    [Fact]
    public void Deserialize_KeepsAnOlderPlanThatHasNoPriorityOrReclaim()
    {
        var plan = DecisionPlanRules.Compose(Sample());
        var node = JsonNode.Parse(DecisionPlanJson.Serialize(plan))!.AsObject();
        var scenario = node["scenario"]!.AsObject();
        scenario.Remove("priority");
        scenario.Remove("constraintDebtId");
        scenario.Remove("constraintKind");
        scenario.Remove("reclaimAmount");

        var read = DecisionPlanJson.Deserialize(node.ToJsonString());

        Assert.Equal(PayoffPriorityChoice.Avalanche, read.Scenario.Priority);
        Assert.Null(read.Scenario.ConstraintDebtId);
        Assert.Null(read.Scenario.ConstraintKind);
        Assert.Equal(0m, read.Scenario.ReclaimAmount);
        Assert.Equal(plan.Kind, read.Kind);
        Assert.Equal(plan.Facts.TotalInterest, read.Facts.TotalInterest);
        Assert.Equal(plan.Debts.Select(debt => debt.Name), read.Debts.Select(debt => debt.Name));
    }

    [Fact]
    public void Save_RejectsAStaleDraftAndKeepsARepeatedDocument()
    {
        var draftId = Guid.NewGuid();

        Assert.Equal(PlanVersionSaveResult.Create, PlanVersionRules.Save(null, null, false));
        Assert.Equal(PlanVersionSaveResult.Stale, PlanVersionRules.Save(null, draftId, false));
        Assert.Equal(PlanVersionSaveResult.Stale, PlanVersionRules.Save(draftId, null, false));
        Assert.Equal(
            PlanVersionSaveResult.Stale,
            PlanVersionRules.Save(draftId, Guid.NewGuid(), false));
        Assert.Equal(PlanVersionSaveResult.Unchanged, PlanVersionRules.Save(draftId, draftId, true));
        Assert.Equal(
            PlanVersionSaveResult.ReplaceDraft,
            PlanVersionRules.Save(draftId, draftId, false));
    }

    [Fact]
    public void Accept_IsIdempotentForTheAcceptedVersionAndStaleAfterItIsReplaced()
    {
        Assert.Equal(PlanVersionAcceptResult.Accept, PlanVersionRules.Accept(true, false));
        Assert.Equal(PlanVersionAcceptResult.AlreadyAccepted, PlanVersionRules.Accept(false, true));
        Assert.Equal(PlanVersionAcceptResult.AlreadyAccepted, PlanVersionRules.Accept(true, true));
        Assert.Equal(PlanVersionAcceptResult.Stale, PlanVersionRules.Accept(false, false));
    }

    #region Private Methods

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
    /// Two debts so a rolled payment has somewhere to go.
    /// </summary>
    private static DecisionPlanInput TwoDebts(PayoffRolloverKind kind)
    {
        var cardId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var loanId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        return Sample() with
        {
            Kind = kind,
            Debts =
            [
                Debt(cardId, "Card", new DateOnly(2027, 4, 15)),
                Debt(loanId, "Loan", new DateOnly(2028, 1, 15))
            ]
        };
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
