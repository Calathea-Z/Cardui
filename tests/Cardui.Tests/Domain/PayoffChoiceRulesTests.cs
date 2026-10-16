using Cardui.Api.Domain.Recovery;
using Cardui.Api.Models;
using Xunit;

namespace Cardui.Tests.Domain;

public class PayoffChoiceRulesTests
{
    private static readonly Guid StoreId = Guid.Parse("70000000-0000-0000-0000-000000000001");
    private static readonly Guid CardId = Guid.Parse("70000000-0000-0000-0000-000000000002");
    private static readonly DateOnly Due = new(2026, 1, 15);

    [Fact]
    public void Alternatives_KeepAvalancheAndTheThreeSupportedOrders()
    {
        var comparison = PayoffPriority.Compare(Input(
            100m,
            [
                Debt(StoreId, "Store", 100m, 10m, 25m),
                Debt(CardId, "Card", 500m, 20m, 25m)
            ]));

        var alternatives = PayoffChoiceRules.Alternatives(comparison);
        Assert.Equal(
            [
                PayoffPriorityChoice.Avalanche,
                PayoffPriorityChoice.MinimumRelease,
                PayoffPriorityChoice.Utilization,
                PayoffPriorityChoice.Constraint
            ],
            alternatives.Select(item => item.Choice));

        var avalanche = alternatives[0];
        var minimum = alternatives[1];
        Assert.False(avalanche.Applied);
        Assert.False(avalanche.ChangesOrder);
        Assert.Equal([CardId, StoreId], avalanche.DebtIds);
        Assert.True(minimum.Applied);
        Assert.True(minimum.ChangesOrder);
        Assert.Equal(3.89m, minimum.InterestDifference);
        Assert.Equal(100m, minimum.TradeoffCash);
        Assert.Equal([StoreId, CardId], minimum.DebtIds);
        Assert.False(alternatives[2].ChangesOrder);
        Assert.False(alternatives[3].ChangesOrder);
        Assert.Equal([StoreId, CardId], PayoffChoiceRules.Order(comparison, PayoffPriorityChoice.MinimumRelease));
        Assert.Equal([CardId, StoreId], PayoffChoiceRules.Order(comparison, PayoffPriorityChoice.Avalanche));
    }

    [Fact]
    public void Order_UsesAPayFirstConstraintWithoutReplacingAvalanche()
    {
        var comparison = PayoffPriority.Compare(Input(
            100m,
            [
                Debt(StoreId, "Store", 100m, 10m, 25m),
                Debt(CardId, "Card", 500m, 20m, 25m)
            ],
            constraints: [new PayoffConstraint(StoreId, PayoffConstraintKind.PayFirst)]));

        var constraint = PayoffChoiceRules.Alternatives(comparison)
            .Single(item => item.Choice == PayoffPriorityChoice.Constraint);
        Assert.True(constraint.Applied);
        Assert.Equal([StoreId, CardId], constraint.DebtIds);
        Assert.Equal([CardId, StoreId], PayoffChoiceRules.Order(comparison, PayoffPriorityChoice.Avalanche));
        Assert.Equal([StoreId, CardId], PayoffChoiceRules.Order(comparison, PayoffPriorityChoice.Constraint));
    }

    [Fact]
    public void SelectedOrder_FeedsRolloverAndKeepsAPartialReclaimUntilTheNextMonth()
    {
        var priority = PayoffPriority.Compare(Input(
            100m,
            [
                Debt(StoreId, "Store", 100m, 10m, 25m),
                Debt(CardId, "Card", 500m, 20m, 25m)
            ]));
        var order = PayoffChoiceRules.Order(priority, PayoffPriorityChoice.MinimumRelease);
        var comparison = PayoffRollover.Compare(new PayoffRolloverInput(
            "USD",
            100m,
            [
                Debt(StoreId, "Store", 100m, 10m, 25m),
                Debt(CardId, "Card", 500m, 20m, 25m)
            ],
            order,
            20m,
            new DateOnly(2026, 1, 1)));

        Assert.Equal(StoreId, comparison.Rollover.DebtIds[0]);
        Assert.Equal(20m, comparison.ReclaimAmount);
        Assert.Equal(0m, comparison.Rollover.CashReclaimed);
        Assert.True(comparison.Reclaim.CashReclaimed > 0);
        Assert.True(comparison.Reclaim.CashRolled > 0);
        Assert.Equal(0m, comparison.ReclaimAll.CashRolled);
        var freed = comparison.Reclaim.FreedPayments[0];
        Assert.True(freed.StartsOn > freed.EndedOn);
    }

    [Fact]
    public void Reject_RequiresADebtAndADirectionForAConstraint()
    {
        Assert.Null(PayoffChoiceRules.Reject(
            PayoffPriorityChoice.Avalanche,
            null,
            null,
            0,
            [StoreId]));
        Assert.Equal(
            "Choose a debt and whether to pay it first or last.",
            PayoffChoiceRules.Reject(
                PayoffPriorityChoice.Constraint,
                null,
                null,
                0,
                [StoreId]));
        Assert.Equal(
            "Choose a debt in this plan.",
            PayoffChoiceRules.Reject(
                PayoffPriorityChoice.Constraint,
                CardId,
                PayoffConstraintKind.PayLast,
                0,
                [StoreId]));
        Assert.Equal(
            "The amount to keep cannot be negative.",
            PayoffChoiceRules.Reject(PayoffPriorityChoice.Avalanche, null, null, -1, [StoreId]));
        Assert.Equal(
            "Enter how much freed cash to keep each month.",
            PayoffChoiceRules.RejectKind(PayoffRolloverKind.Reclaim, 0));
        Assert.Null(PayoffChoiceRules.RejectKind(PayoffRolloverKind.ReclaimAll, 0));
        Assert.Null(PayoffChoiceRules.RejectKind(PayoffRolloverKind.Reclaim, 25));
    }

    #region Private Methods

    private static PayoffPriorityInput Input(
        decimal extra,
        IReadOnlyList<PayoffDebt> debts,
        IReadOnlyList<PayoffConstraint>? constraints = null)
    {
        return new PayoffPriorityInput("USD", extra, debts, [], constraints ?? []);
    }

    private static PayoffDebt Debt(
        Guid id,
        string name,
        decimal balance,
        decimal apr,
        decimal minimum)
    {
        return new PayoffDebt(
            new DebtAmortizationInput(
                id,
                name,
                "USD",
                DebtKind.Revolving,
                balance,
                apr,
                null,
                null,
                minimum,
                null,
                Due,
                0m),
            2000m);
    }

    #endregion
}
