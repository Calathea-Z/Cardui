using Cardui.Api.Domain.Recovery;
using Cardui.Api.Models;
using Xunit;

namespace Cardui.Tests.Domain;

public class PlanCashScenarioTests
{
    private static readonly Guid PayId = Guid.Parse("71000000-0000-0000-0000-000000000001");
    private static readonly Guid SpendingId = Guid.Parse("71000000-0000-0000-0000-000000000002");
    private static readonly Guid CardId = Guid.Parse("71000000-0000-0000-0000-000000000003");
    private static readonly DateOnly Start = new(2026, 1, 1);

    [Fact]
    public void Apply_LeavesTheSavedPlanUnchanged()
    {
        var input = Input(reserve: 200m, savings: [Spending(Start, 300m)]);

        var adjusted = PlanCashScenarioAdjustment.Apply(input, 300m, 0m, PlanCashScenario.None);

        Assert.Equal(200m, adjusted.StartingReserve);
        Assert.Null(adjusted.OmitIncomeBefore);
        Assert.Null(adjusted.Windfall);
        Assert.Equal(300m, Assert.Single(adjusted.Savings).Amount);
    }

    [Fact]
    public void IncomeLoss_OmitsPayUntilTheResumeDateAndKeepsALaterRaise()
    {
        var income = new HouseholdIncome(
            PayId,
            "Pay",
            "USD",
            1000m,
            800m,
            IncomeCadence.Monthly,
            new DateOnly(2026, 1, 5),
            [new DatedIncomeRaise(new DateOnly(2026, 3, 1), 1200m)],
            null);
        var scenario = PlanCashScenario.None with { IncomeLossMonths = 2 };
        var adjusted = PlanCashScenarioAdjustment.Apply(Input(incomes: [income]), 0m, 0m, scenario);

        var outlook = Project(adjusted);
        var januaryPay = outlook.Rollover.Typical.Days
            .Single(day => day.Date == new DateOnly(2026, 1, 5))
            .Events
            .Where(item => item.Kind == CashFlowKind.Income);

        Assert.Equal(new DateOnly(2026, 3, 1), adjusted.OmitIncomeBefore);
        Assert.Empty(januaryPay);
        Assert.Equal(4800m, outlook.Rollover.Typical.Horizons[0].Window.EndingCash);
        Assert.Equal(3200m, outlook.Rollover.LowPay!.Horizons[0].Window.EndingCash);
    }

    [Fact]
    public void Windfall_AddsCashOnItsDateAndDoesNotChangeThePayoffOrReserve()
    {
        var debt = new PayoffDebt(
            new DebtAmortizationInput(
                CardId,
                "Card",
                "USD",
                DebtKind.Revolving,
                100m,
                0m,
                null,
                null,
                50m,
                null,
                new DateOnly(2026, 1, 15),
                0m),
            null);
        var comparison = PayoffRollover.Compare(new PayoffRolloverInput("USD", 0m, [debt], [], 0m, Start));
        var scenario = PlanCashScenario.None with
        {
            WindfallAmount = 250m,
            WindfallOn = new DateOnly(2026, 1, 10)
        };
        var adjusted = PlanCashScenarioAdjustment.Apply(Input(cash: 1000m, reserve: 100m), 0m, 0m, scenario);

        var without = HouseholdCashOutlook.Project(Input(cash: 1000m, reserve: 100m), [debt], comparison);
        var withWindfall = HouseholdCashOutlook.Project(adjusted, [debt], comparison);

        Assert.Equal(without.Rollover.Typical.Horizons[0].Debts.Single().PaidOffOn, withWindfall.Rollover.Typical.Horizons[0].Debts.Single().PaidOffOn);
        Assert.Equal(100m, withWindfall.StartingReserve);
        Assert.Equal(1250m, CashOn(withWindfall.Rollover.Typical, new DateOnly(2026, 1, 10)));
        Assert.Equal(100m, withWindfall.Rollover.Typical.Days.Single(day => day.Date == new DateOnly(2026, 1, 10)).Reserve);
    }

    [Fact]
    public void SpendingChange_ReplacesFullMonthsAndKeepsASmallerRemainder()
    {
        var savings = new[]
        {
            Spending(new DateOnly(2026, 1, 20), 40m),
            Spending(new DateOnly(2026, 2, 1), 300m)
        };
        var scenario = PlanCashScenario.None with { LivingSpendingMonthly = 100m };
        var adjusted = PlanCashScenarioAdjustment.Apply(
            Input(asOf: new DateOnly(2026, 1, 20), cash: 1000m, savings: savings),
            300m,
            0m,
            scenario);

        var outlook = Project(adjusted);
        Assert.Equal(40m, Assert.Single(adjusted.Savings, item => item.Date == new DateOnly(2026, 1, 20)).Amount);
        Assert.Equal(100m, Assert.Single(adjusted.Savings, item => item.Date == new DateOnly(2026, 2, 1)).Amount);
        Assert.Equal(960m, CashOn(outlook.Rollover.Typical, new DateOnly(2026, 1, 20)));
        Assert.Equal(860m, CashOn(outlook.Rollover.Typical, new DateOnly(2026, 2, 1)));
    }

    [Fact]
    public void CashFloor_ReplacesOnlyTheFloorInsideTheReserve()
    {
        var scenario = PlanCashScenario.None with { CashFloor = 100m };
        var adjusted = PlanCashScenarioAdjustment.Apply(Input(cash: 1000m, reserve: 700m), 0m, 500m, scenario);

        var outlook = Project(adjusted);

        Assert.Equal(300m, outlook.StartingReserve);
        Assert.Equal(700m, outlook.StartingAvailable);
        Assert.Equal(1000m, CashOn(outlook.Rollover.Typical, Start));
    }

    [Fact]
    public void Reject_AcceptsAWindfallOnTheLastForecastDayAndRejectsTheNextDay()
    {
        var today = new DateOnly(2026, 1, 1);

        Assert.Null(PlanCashScenarioRules.Reject(0m, 18, 25m, new DateOnly(2027, 6, 30), null, null, 0m, today));
        Assert.Equal(
            "A windfall has to fall inside the 18-month forecast.",
            PlanCashScenarioRules.Reject(0m, 0, 25m, new DateOnly(2027, 7, 1), null, null, 0m, today));
    }

    [Fact]
    public void Reject_NamesTheFirstUnusableAmount()
    {
        var today = Start;

        Assert.Equal(
            "Extra each month cannot be negative.",
            PlanCashScenarioRules.Reject(-1m, 0, 0m, null, null, null, 0m, today));
        Assert.Equal(
            "Income loss has to be from 0 to 18 months.",
            PlanCashScenarioRules.Reject(0m, 19, 0m, null, null, null, 0m, today));
        Assert.Equal(
            "Choose the date the windfall arrives.",
            PlanCashScenarioRules.Reject(0m, 0, 25m, null, null, null, 0m, today));
        Assert.Equal(
            "Set flexible monthly spending on Plan budget before trying a different amount.",
            PlanCashScenarioRules.Reject(0m, 0, 0m, null, 100m, null, 0m, today));
        Assert.Null(PlanCashScenarioRules.Reject(0m, 0, 0m, null, 0m, 0m, 300m, today));
        Assert.Equal(
            "Cash to keep cannot be negative.",
            PlanCashScenarioRules.Reject(0m, 0, 0m, null, null, -1m, 0m, today));
    }

    #region Private Methods

    /// <summary>
    /// Projects the adjusted facts with no debts so the cash days are the scenario itself.
    /// </summary>
    private static HouseholdCashOutlookReport Project(HouseholdCashOutlookInput input)
    {
        var comparison = PayoffRollover.Compare(new PayoffRolloverInput(
            "USD",
            0m,
            [],
            [],
            0m,
            input.AsOf));
        return HouseholdCashOutlook.Project(input, [], comparison);
    }

    /// <summary>
    /// Ending cash on one walked day.
    /// </summary>
    private static decimal CashOn(CashForecastReport forecast, DateOnly date)
    {
        return forecast.Days.Single(day => day.Date == date).Cash;
    }

    /// <summary>
    /// One flexible-spending event. The amount is what the saved schedule would already have produced.
    /// </summary>
    private static CashFlowEvent Spending(DateOnly date, decimal amount)
    {
        return new CashFlowEvent(date, CashFlowKind.LivingSpending, SpendingId, "Monthly living spending", amount, "USD");
    }

    /// <summary>
    /// Household cash facts before a preview is applied.
    /// </summary>
    private static HouseholdCashOutlookInput Input(
        decimal cash = 0m,
        decimal reserve = 0m,
        DateOnly? asOf = null,
        IReadOnlyList<HouseholdIncome>? incomes = null,
        IReadOnlyList<CashFlowEvent>? savings = null)
    {
        return new HouseholdCashOutlookInput(
            "USD",
            asOf ?? Start,
            cash,
            incomes ?? [],
            [],
            reserve,
            savings ?? []);
    }

    #endregion
}
