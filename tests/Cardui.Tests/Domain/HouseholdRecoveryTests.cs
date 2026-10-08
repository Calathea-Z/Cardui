using Cardui.Api.Domain.Recovery;
using Cardui.Api.Models;
using Xunit;

namespace Cardui.Tests.Domain;

public class HouseholdRecoveryTests
{
    private static readonly Guid CardId = Guid.Parse("60000000-0000-0000-0000-000000000001");
    private static readonly Guid StoreId = Guid.Parse("60000000-0000-0000-0000-000000000002");
    private static readonly Guid BlankId = Guid.Parse("60000000-0000-0000-0000-000000000003");

    [Fact]
    public void Prepare_UsesTheBalanceInUseAndNoExtra()
    {
        var input = HouseholdRecovery.Prepare(
            "USD",
            [
                Debt(
                    CardId,
                    "Card",
                    balance: 180m,
                    apr: 19.99m,
                    minimum: 40m,
                    due: new DateOnly(2026, 1, 15),
                    limit: 1000m)
            ]);

        var debt = Assert.Single(input.Debts);
        Assert.Equal(180m, debt.Terms.Balance);
        Assert.Equal(0m, debt.Terms.ExtraPayment);
        Assert.Equal(1000m, debt.CreditLimit);
        Assert.Equal(0m, input.MonthlyExtra);
        Assert.Equal(0m, input.ReclaimAmount);
        Assert.Empty(input.Order);
    }

    [Fact]
    public void Prepare_LeavesAnotherCurrencyForTheExclusionRule()
    {
        var input = HouseholdRecovery.Prepare(
            "USD",
            [
                Debt(CardId, "Card", balance: 100m, apr: 12m, minimum: 25m, due: new DateOnly(2026, 1, 15)),
                Debt(
                    StoreId,
                    "Store",
                    balance: 50m,
                    apr: 0m,
                    minimum: 10m,
                    due: new DateOnly(2026, 1, 15),
                    currency: "CAD")
            ]);

        Assert.Contains(input.Debts, debt => debt.Terms.DebtId == StoreId && debt.Terms.Currency == "CAD");

        var recovery = CashFlowRecovery.Track(PayoffRollover.Compare(input));
        Assert.Equal(["CAD"], recovery.ExcludedCurrencies);
        Assert.DoesNotContain(recovery.Rollover.Steps, step => step.DebtId == StoreId);
    }

    [Fact]
    public void Prepare_KeepsAMissingRateMinimumOrDueDateUnknown()
    {
        var input = HouseholdRecovery.Prepare(
            "USD",
            [Debt(CardId, "Card", balance: 100m, apr: null, minimum: null, due: null)]);

        var debt = Assert.Single(input.Debts);
        Assert.Null(debt.Terms.Apr);
        Assert.Null(debt.Terms.MinimumPayment);
        Assert.Null(debt.Terms.NextDueDate);
        Assert.Equal(100m, debt.Terms.Balance);

        var recovery = CashFlowRecovery.Track(PayoffRollover.Compare(input));
        Assert.Contains(
            "missing a rate, minimum, or due date",
            recovery.Rollover.Explanation,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Prepare_LeavesOutAMissingBalance()
    {
        var input = HouseholdRecovery.Prepare(
            "USD",
            [
                Debt(CardId, "Card", balance: 100m, apr: 12m, minimum: 25m, due: new DateOnly(2026, 1, 15)),
                Debt(BlankId, "Blank", balance: null, apr: 12m, minimum: 25m, due: new DateOnly(2026, 1, 15))
            ]);

        Assert.DoesNotContain(input.Debts, debt => debt.Terms.DebtId == BlankId);
        Assert.Equal(CardId, Assert.Single(input.Debts).Terms.DebtId);
    }

    private static HouseholdRecoveryDebt Debt(
        Guid id,
        string name,
        decimal? balance,
        decimal? apr,
        decimal? minimum,
        DateOnly? due,
        string currency = "USD",
        decimal? limit = null)
    {
        return new HouseholdRecoveryDebt(
            id,
            name,
            currency,
            DebtKind.Revolving,
            balance,
            apr,
            null,
            null,
            minimum,
            null,
            due,
            limit);
    }
}
