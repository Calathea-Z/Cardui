using Cardui.Api.Domain.Recovery;
using Cardui.Api.Models;
using Xunit;

namespace Cardui.Tests.Domain;

public class SpendableRulesTests
{
    private static readonly Guid PayId = Guid.Parse("72000000-0000-0000-0000-000000000001");
    private static readonly Guid SideId = Guid.Parse("72000000-0000-0000-0000-000000000002");
    private static readonly Guid RentId = Guid.Parse("72000000-0000-0000-0000-000000000003");
    private static readonly Guid CardId = Guid.Parse("72000000-0000-0000-0000-000000000004");
    private static readonly Guid SpendingId = Guid.Parse("72000000-0000-0000-0000-000000000005");
    private static readonly DateOnly Start = new(2026, 1, 1);

    [Fact]
    public void Project_UsesTheLowestDayRatherThanMonthlySurplus()
    {
        var estimate = Project(Input(
            cash: 50m,
            incomes: [Income("Pay", 1000m, new DateOnly(2026, 1, 5))],
            bills: [Bill(400m, new DateOnly(2026, 1, 20))])).Rollover;

        Assert.Equal(new DateOnly(2026, 1, 30), estimate.Through);
        Assert.Equal(50m, estimate.Spendable);
        Assert.Equal(50m, estimate.LowestAvailable);
        Assert.Equal(Start, estimate.LowestOn);
        Assert.Equal(1000m, estimate.ReliableIncome);
        Assert.Equal(400m, estimate.Obligations);
        Assert.False(estimate.AlreadyShort);
        Assert.False(estimate.Guardrail.Active);
    }

    [Fact]
    public void Project_IsShortWhenABillLandsBeforeSteadyPay()
    {
        var estimate = Project(Input(
            cash: 100m,
            incomes: [Income("Pay", 1000m, new DateOnly(2026, 1, 20))],
            bills: [Bill(400m, new DateOnly(2026, 1, 5))])).Rollover;

        Assert.Equal(0m, estimate.Spendable);
        Assert.Equal(-300m, estimate.LowestAvailable);
        Assert.Equal(new DateOnly(2026, 1, 5), estimate.LowestOn);
        Assert.True(estimate.AlreadyShort);
        Assert.Equal(1000m, estimate.ReliableIncome);
        Assert.Equal(400m, estimate.Obligations);
    }

    [Fact]
    public void Project_KeepsReservedFundsOutOfSpendableCash()
    {
        var estimate = Project(Input(cash: 500m, reserve: 400m)).Rollover;

        Assert.Equal(100m, estimate.Spendable);
        Assert.Equal(400m, estimate.Reserved);
        Assert.Equal(0m, estimate.ReliableIncome);
        Assert.Equal(0m, estimate.Obligations);
    }

    [Fact]
    public void Project_LeavesOutVariableAndUncertainPay()
    {
        var estimate = Project(Input(
            cash: 100m,
            incomes:
            [
                Income("Side work", 2000m, new DateOnly(2026, 1, 2), IncomeReliability.Variable),
                Income("Maybe", 500m, new DateOnly(2026, 1, 3), IncomeReliability.Uncertain)
            ],
            bills: [Bill(150m, new DateOnly(2026, 1, 10))])).Rollover;

        Assert.Equal(0m, estimate.Spendable);
        Assert.Equal(-50m, estimate.LowestAvailable);
        Assert.Equal(0m, estimate.ReliableIncome);
        Assert.Contains(
            estimate.Uncertainties,
            item => item.Kind == SpendableUncertaintyKind.VariableIncome && item.Name == "Side work");
        Assert.Contains(
            estimate.Uncertainties,
            item => item.Kind == SpendableUncertaintyKind.UncertainIncome && item.Name == "Maybe");
        Assert.Contains(
            estimate.Uncertainties,
            item => item.Kind == SpendableUncertaintyKind.NoReliableIncome);
    }

    [Fact]
    public void Project_LeavesOutAWindfallAndPayThePreviewOmits()
    {
        var input = Input(
            cash: 100m,
            incomes: [Income("Pay", 1000m, new DateOnly(2026, 1, 5))]) with
        {
            OmitIncomeBefore = new DateOnly(2026, 2, 1),
            Windfall = new CashFlowEvent(new DateOnly(2026, 1, 8), CashFlowKind.Income, PayId, "Bonus", 5000m, "USD")
        };

        var estimate = Project(input).Rollover;

        Assert.Equal(100m, estimate.Spendable);
        Assert.Equal(0m, estimate.ReliableIncome);
        Assert.Contains(estimate.Uncertainties, item => item.Kind == SpendableUncertaintyKind.WindfallLeftOut);
        Assert.Contains(estimate.Uncertainties, item => item.Kind == SpendableUncertaintyKind.IncomeLeftOut);
    }

    [Fact]
    public void Project_NamesAnUnsetOrdinarySpendingAmountAndAnotherCurrency()
    {
        var estimate = Project(
            Input(incomes: [Income("Euro pay", 1000m, new DateOnly(2026, 1, 5), currency: "EUR")]),
            ordinarySpendingKnown: false).Rollover;

        Assert.Contains(
            estimate.Uncertainties,
            item => item.Kind == SpendableUncertaintyKind.OrdinarySpendingUnset);
        Assert.Contains(
            estimate.Uncertainties,
            item => item.Kind == SpendableUncertaintyKind.ExcludedCurrency && item.Name == "EUR");
        Assert.Contains(estimate.Uncertainties, item => item.Kind == SpendableUncertaintyKind.NoReliableIncome);
    }

    [Fact]
    public void Project_LeavesOutADebtWhoseMinimumIsUnknown()
    {
        var unknown = new PayoffDebt(
            new DebtAmortizationInput(
                CardId,
                "Store card",
                "USD",
                DebtKind.Revolving,
                800m,
                18m,
                null,
                null,
                null,
                null,
                new DateOnly(2026, 1, 10),
                0m),
            null);

        var estimate = Project(Input(), debts: [unknown]).Rollover;

        Assert.Contains(
            estimate.Uncertainties,
            item => item.Kind == SpendableUncertaintyKind.DebtLeftOut && item.Name == "Store card");
        Assert.Equal(0m, estimate.Obligations);
    }

    [Fact]
    public void Project_WarnsWhenExtraLeavesOrdinarySpendingShort()
    {
        var card = Debt(CardId, "Card", 1000m, 50m, new DateOnly(2026, 1, 10));
        var spending = new CashFlowEvent(
            new DateOnly(2026, 1, 20),
            CashFlowKind.LivingSpending,
            SpendingId,
            "Ordinary spending",
            100m,
            "USD");
        var report = Project(
            Input(cash: 300m, savings: [spending]),
            debts: [card],
            monthlyExtra: 200m,
            ordinarySpendingKnown: true);

        var guard = report.Rollover.Guardrail;
        Assert.True(guard.Active);
        Assert.True(guard.OrdinaryExpenses);
        Assert.False(guard.NearTermObligations);
        Assert.Equal(new DateOnly(2026, 1, 20), guard.On);
        Assert.Equal(50m, guard.Shortfall);
        Assert.False(guard.AlreadyShortWithoutExtra);
        Assert.Equal(100m, report.Rollover.OrdinarySpending);
        Assert.False(report.Rollover.AlreadyShort);
    }

    [Fact]
    public void Project_DoesNotWarnWhenTheExtraStillCoversNearTermBills()
    {
        var card = Debt(CardId, "Card", 1000m, 50m, new DateOnly(2026, 1, 10));
        var spending = new CashFlowEvent(
            new DateOnly(2026, 1, 20),
            CashFlowKind.LivingSpending,
            SpendingId,
            "Ordinary spending",
            100m,
            "USD");
        var guard = Project(
            Input(cash: 1000m, savings: [spending]),
            debts: [card],
            monthlyExtra: 50m,
            ordinarySpendingKnown: true).Rollover.Guardrail;

        Assert.False(guard.Active);
    }

    private static SpendableReport Project(
        HouseholdCashOutlookInput input,
        IReadOnlyList<PayoffDebt>? debts = null,
        decimal monthlyExtra = 0m,
        bool ordinarySpendingKnown = true,
        IReadOnlyList<string>? missing = null)
    {
        var list = debts ?? [];
        var aggressive = PayoffRollover.Compare(Rollover(monthlyExtra, list));
        var minimums = monthlyExtra > 0
            ? PayoffRollover.Compare(Rollover(0m, list))
            : aggressive;
        return SpendableRules.Project(
            input,
            list.Select(debt => debt.Terms).ToList(),
            aggressive,
            minimums,
            missing ?? [],
            ordinarySpendingKnown);
    }

    private static HouseholdCashOutlookInput Input(
        decimal cash = 0m,
        decimal reserve = 0m,
        IReadOnlyList<HouseholdIncome>? incomes = null,
        IReadOnlyList<DatedBill>? bills = null,
        IReadOnlyList<CashFlowEvent>? savings = null)
    {
        return new HouseholdCashOutlookInput(
            "USD",
            Start,
            cash,
            incomes ?? [],
            bills ?? [],
            reserve,
            savings ?? []);
    }

    private static HouseholdIncome Income(
        string name,
        decimal amount,
        DateOnly next,
        IncomeReliability reliability = IncomeReliability.Steady,
        string currency = "USD")
    {
        return new HouseholdIncome(PayId, name, currency, amount, null, IncomeCadence.Monthly, next, [], null, reliability);
    }

    private static DatedBill Bill(decimal amount, DateOnly due)
    {
        return new DatedBill(RentId, "Rent", "USD", amount, ObligationCadence.Monthly, due);
    }

    private static PayoffRolloverInput Rollover(decimal extra, IReadOnlyList<PayoffDebt> debts)
    {
        return new PayoffRolloverInput("USD", extra, debts, [], 0m, Start);
    }

    private static PayoffDebt Debt(Guid id, string name, decimal balance, decimal minimum, DateOnly due)
    {
        return new PayoffDebt(
            new DebtAmortizationInput(
                id,
                name,
                "USD",
                DebtKind.Revolving,
                balance,
                0m,
                null,
                null,
                minimum,
                null,
                due,
                0m),
            null);
    }
}
