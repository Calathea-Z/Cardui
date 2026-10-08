using Cardui.Api.Domain.Recovery;
using Cardui.Api.Models;
using Xunit;

namespace Cardui.Tests.Domain;

public class HouseholdCashOutlookTests
{
    private static readonly Guid StoreId = Guid.Parse("70000000-0000-0000-0000-000000000001");
    private static readonly Guid CardId = Guid.Parse("70000000-0000-0000-0000-000000000002");
    private static readonly Guid PayId = Guid.Parse("70000000-0000-0000-0000-000000000003");
    private static readonly Guid RentId = Guid.Parse("70000000-0000-0000-0000-000000000004");
    private static readonly DateOnly Start = new(2026, 1, 1);

    [Fact]
    public void Rollover_DoesNotReplayADueDateBeforeTheStartAndKeepsTheStoredDay()
    {
        var comparison = PayoffRollover.Compare(Rollover(
            new DateOnly(2026, 3, 10),
            [Debt(StoreId, "Store", 100m, 25m, new DateOnly(2026, 1, 31))]));

        var path = comparison.Rollover;
        Assert.Equal(
            [new DateOnly(2026, 3, 31), new DateOnly(2026, 4, 30), new DateOnly(2026, 5, 31), new DateOnly(2026, 6, 30)],
            path.BalancePoints.Select(point => point.DueDate));
        Assert.Equal(new DateOnly(2026, 6, 30), path.PaidOffOn);
        Assert.Equal(new DateOnly(2026, 7, 31), Assert.Single(path.FreedPayments).StartsOn);
    }

    [Fact]
    public void PayoffPathAndForecast_AgreeOnAPastDueDate()
    {
        var asOf = new DateOnly(2026, 1, 10);
        var store = Debt(StoreId, "Store", 100m, 25m, new DateOnly(2025, 11, 30));
        var comparison = PayoffRollover.Compare(Rollover(asOf, [store]));
        var forecast = CashForecast.Project(new CashForecastInput(
            "USD",
            ForecastIncomeBasis.Typical,
            asOf,
            0m,
            0m,
            [],
            [],
            [store.Terms],
            []));

        Assert.Equal(new DateOnly(2026, 1, 30), comparison.Rollover.BalancePoints[0].DueDate);
        Assert.Equal(new DateOnly(2026, 4, 30), comparison.Rollover.PaidOffOn);
        Assert.Equal(
            comparison.Rollover.PaidOffOn,
            forecast.Milestones.Single(item => item.Kind == ForecastMilestoneKind.DebtPaidOff).Date);
        Assert.Equal(comparison.Rollover.PaidOffOn, forecast.Horizons[0].Debts.Single().PaidOffOn);
        Assert.Equal([new DateOnly(2026, 1, 30)], DebtPaymentDates(forecast));
    }

    [Fact]
    public void Project_RolloverKeepsAFreedMinimumInDebtPaymentsAndKeepingReturnsItToCash()
    {
        var debts = new[]
        {
            Debt(StoreId, "Store", 50m, 50m, new DateOnly(2026, 1, 15)),
            Debt(CardId, "Card", 1000m, 25m, new DateOnly(2026, 1, 20))
        };
        var comparison = PayoffRollover.Compare(Rollover(Start, debts));

        var outlook = HouseholdCashOutlook.Project(Outlook(cash: 1000m), debts, comparison);

        Assert.Equal(550m, outlook.Rollover.Typical.Horizons[0].Window.EndingCash);
        Assert.Equal(800m, outlook.ReclaimAll.Typical.Horizons[0].Window.EndingCash);
        Assert.Equal(PayoffRolloverKind.Rollover, outlook.Rollover.Kind);
        Assert.Equal(PayoffRolloverKind.ReclaimAll, outlook.ReclaimAll.Kind);
        Assert.Equal(1000m, outlook.StartingCash);
        Assert.Equal(Start, outlook.AsOf);
        Assert.False(outlook.HasIncome);
        Assert.False(outlook.HasBills);
    }

    [Fact]
    public void Project_LowPayUsesTheLowAmountAndLeavesOutRaises()
    {
        var income = new HouseholdIncome(
            PayId,
            "Pay",
            "USD",
            1000m,
            800m,
            IncomeCadence.Monthly,
            new DateOnly(2026, 1, 5),
            [new DatedIncomeRaise(new DateOnly(2026, 3, 1), 1200m)]);
        var comparison = PayoffRollover.Compare(Rollover(Start, []));

        var outlook = HouseholdCashOutlook.Project(
            Outlook(incomes: [income], bills: [new DatedBill(RentId, "Rent", "USD", 100m, ObligationCadence.Monthly, new DateOnly(2026, 1, 10))]),
            [],
            comparison);

        Assert.Equal(6800m - 600m, outlook.Rollover.Typical.Horizons[0].Window.EndingCash);
        Assert.Equal(4800m - 600m, outlook.Rollover.LowPay!.Horizons[0].Window.EndingCash);
        Assert.Equal(ForecastIncomeBasis.Conservative, outlook.Rollover.LowPay.IncomeBasis);
        Assert.True(outlook.HasIncome);
        Assert.True(outlook.HasBills);
    }

    [Fact]
    public void Project_HasNoLowPayForecastWhenNoSourceRecordsALowAmount()
    {
        var income = new HouseholdIncome(
            PayId,
            "Pay",
            "USD",
            1000m,
            null,
            IncomeCadence.Monthly,
            new DateOnly(2026, 1, 5),
            []);

        var outlook = HouseholdCashOutlook.Project(
            Outlook(incomes: [income]),
            [],
            PayoffRollover.Compare(Rollover(Start, [])));

        Assert.Null(outlook.Rollover.LowPay);
        Assert.Null(outlook.ReclaimAll.LowPay);
    }

    [Fact]
    public void Project_KeepsCashAndRaisesTheReserveWhenMoneyIsSetAside()
    {
        var goalId = Guid.Parse("70000000-0000-0000-0000-000000000005");
        var outlook = HouseholdCashOutlook.Project(
            new HouseholdCashOutlookInput(
                "USD",
                Start,
                1000m,
                [],
                [],
                200m,
                [new CashFlowEvent(Start, CashFlowKind.Savings, goalId, "Emergency", 50m, "USD")]),
            [],
            PayoffRollover.Compare(Rollover(Start, [])));

        var day = outlook.Rollover.Typical.Days.Single(item => item.Date == Start);
        Assert.Equal(200m, outlook.StartingReserve);
        Assert.Equal(800m, outlook.StartingAvailable);
        Assert.Equal(1000m, day.Cash);
        Assert.Equal(250m, day.Reserve);
        Assert.Equal(750m, day.Available);
        Assert.False(day.ReserveShortfall);
    }

    [Fact]
    public void Project_EverydaySpendingLeavesCashAndDoesNotRaiseTheReserve()
    {
        var goalId = Guid.Parse("70000000-0000-0000-0000-000000000006");
        var outlook = HouseholdCashOutlook.Project(
            new HouseholdCashOutlookInput(
                "USD",
                Start,
                1000m,
                [],
                [],
                0m,
                [new CashFlowEvent(Start, CashFlowKind.EverydaySpending, goalId, "Everyday spending", 300m, "USD")]),
            [],
            PayoffRollover.Compare(Rollover(Start, [])));

        var day = outlook.Rollover.Typical.Days.Single(item => item.Date == Start);
        Assert.Equal(0m, outlook.StartingReserve);
        Assert.Equal(700m, day.Cash);
        Assert.Equal(0m, day.Reserve);
        Assert.Equal(700m, day.Available);
    }

    private static IEnumerable<DateOnly> DebtPaymentDates(CashForecastReport forecast)
    {
        return forecast.Days
            .SelectMany(day => day.Events)
            .Where(item => item.Kind == CashFlowKind.DebtPayment)
            .Select(item => item.Date);
    }

    private static HouseholdCashOutlookInput Outlook(
        decimal cash = 0m,
        IReadOnlyList<HouseholdIncome>? incomes = null,
        IReadOnlyList<DatedBill>? bills = null)
    {
        return new HouseholdCashOutlookInput(
            "USD",
            Start,
            cash,
            incomes ?? [],
            bills ?? [],
            0m,
            []);
    }

    private static PayoffRolloverInput Rollover(DateOnly asOf, IReadOnlyList<PayoffDebt> debts)
    {
        return new PayoffRolloverInput("USD", 0m, debts, [], 0m, asOf);
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
