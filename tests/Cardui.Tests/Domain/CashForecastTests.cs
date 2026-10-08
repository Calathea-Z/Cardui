using Cardui.Api.Domain.Recovery;
using Cardui.Api.Models;
using Xunit;

namespace Cardui.Tests.Domain;

public class CashForecastTests
{
    private static readonly Guid PayId = Guid.Parse("30000000-0000-0000-0000-000000000001");
    private static readonly Guid RentId = Guid.Parse("30000000-0000-0000-0000-000000000002");
    private static readonly Guid LoanId = Guid.Parse("30000000-0000-0000-0000-000000000003");
    private static readonly DateOnly Start = new(2026, 1, 1);

    [Fact]
    public void Project_KeepsThreeBiweeklyPaychecksOnTheirDates()
    {
        var report = CashForecast.Project(Input(
            incomes: [Pay("Pay", 1000m, IncomeCadence.Biweekly, new DateOnly(2026, 1, 2))]));

        var paid = report.Days.SelectMany(day => day.Events).ToList();
        Assert.Equal(30, report.Days.Count);
        Assert.Equal(new DateOnly(2026, 1, 30), report.DayView.Through);
        Assert.Equal(
            [
                new DateOnly(2026, 1, 2),
                new DateOnly(2026, 1, 16),
                new DateOnly(2026, 1, 30)
            ],
            paid.Select(item => item.Date));
        Assert.Equal(3000m, report.DayView.EndingCash);
        Assert.Equal(13000m, report.Horizons[0].Window.EndingCash);
        Assert.Equal([6, 12, 18], report.Horizons.Select(horizon => horizon.Months));
        Assert.DoesNotContain(paid, item => item.Amount == 2166.67m);
        Assert.DoesNotContain(report.Assumptions, line => line.Contains("another currency", StringComparison.Ordinal));
    }

    [Fact]
    public void Project_AppliesARaiseOnAndAfterItsDate()
    {
        var report = CashForecast.Project(Input(
            asOf: new DateOnly(2026, 1, 30),
            incomes:
            [
                Pay(
                    "Pay",
                    1000m,
                    IncomeCadence.Biweekly,
                    new DateOnly(2026, 1, 2),
                    [new DatedIncomeRaise(new DateOnly(2026, 2, 1), 1100m)])
            ]));

        var paid = report.Days.SelectMany(day => day.Events).ToList();
        Assert.Equal(1000m, paid.Single(item => item.Date == new DateOnly(2026, 1, 30)).Amount);
        Assert.Equal(1100m, paid.Single(item => item.Date == new DateOnly(2026, 2, 13)).Amount);
        Assert.Equal(1100m, paid.Single(item => item.Date == new DateOnly(2026, 2, 27)).Amount);
        Assert.Equal(3200m, report.DayView.EndingCash);
    }

    [Fact]
    public void Project_ShowsAShortfallWhenTheBillComesBeforePayday()
    {
        var report = CashForecast.Project(Input(
            cash: 100m,
            incomes: [Pay("Pay", 1000m, IncomeCadence.Irregular, new DateOnly(2026, 1, 9))],
            bills: [Bill("Rent", 500m, ObligationCadence.Irregular, new DateOnly(2026, 1, 5))]));

        var shortDay = report.Days.Single(day => day.Date == new DateOnly(2026, 1, 5));
        var payday = report.Days.Single(day => day.Date == new DateOnly(2026, 1, 9));
        Assert.Equal(-400m, shortDay.Cash);
        Assert.True(shortDay.CashShortfall);
        Assert.False(shortDay.ReserveShortfall);
        Assert.Equal(600m, payday.Cash);
        Assert.Equal(-400m, report.DayView.LowestCash);
        Assert.Equal(new DateOnly(2026, 1, 5), report.DayView.LowestCashOn);
        Assert.True(report.Horizons[0].Window.CashShortfall);
        Assert.Equal(
            [
                new ForecastMilestone(new DateOnly(2026, 1, 5), ForecastMilestoneKind.CashShortfall, null, "Cash", -400m),
                new ForecastMilestone(new DateOnly(2026, 1, 9), ForecastMilestoneKind.CashRecovered, null, "Cash", 600m)
            ],
            report.Milestones);
    }

    [Fact]
    public void Project_CoversABillWithSameDayIncome()
    {
        var report = CashForecast.Project(Input(
            asOf: new DateOnly(2026, 1, 15),
            incomes: [Pay("Pay", 1000m, IncomeCadence.Monthly, new DateOnly(2026, 1, 15))],
            bills: [Bill("Rent", 800m, ObligationCadence.Monthly, new DateOnly(2026, 1, 15))]));

        var day = report.Days[0];
        Assert.Equal([CashFlowKind.Income, CashFlowKind.Bill], day.Events.Select(item => item.Kind));
        Assert.Equal(200m, day.Cash);
        Assert.Empty(report.Milestones);
        Assert.False(report.DayView.CashShortfall);
    }

    [Fact]
    public void Project_MovesSavingsIntoTheReserveWithoutReducingCash()
    {
        var report = CashForecast.Project(Input(
            cash: 500m,
            reserve: 200m,
            bills: [Bill("Rent", 400m, ObligationCadence.Irregular, Start)],
            savings: [Save("Buffer", 50m, new DateOnly(2026, 1, 2))]));

        var first = report.Days[0];
        var second = report.Days[1];
        Assert.Equal(100m, first.Cash);
        Assert.Equal(-100m, first.Available);
        Assert.True(first.ReserveShortfall);
        Assert.False(first.CashShortfall);
        Assert.Equal(100m, second.Cash);
        Assert.Equal(250m, second.Reserve);
        Assert.Equal(-150m, second.Available);
        Assert.Equal(
            new ForecastMilestone(Start, ForecastMilestoneKind.ReserveShortfall, null, "Reserve", -100m),
            Assert.Single(report.Milestones));
    }

    [Fact]
    public void Project_PaysTheKnownLoanAndClearsItByTwelveMonths()
    {
        var debt = Loan(1000m, 12m, null, remainingTerm: 12, due: new DateOnly(2026, 1, 15));
        var report = CashForecast.Project(Input(
            asOf: new DateOnly(2026, 1, 15),
            cash: 10000m,
            debts: [debt]));

        var six = report.Horizons.Single(horizon => horizon.Months == 6);
        var twelve = report.Horizons.Single(horizon => horizon.Months == 12);
        var schedule = DebtAmortization.Project(debt, six.Window.Through, new DateOnly(2026, 1, 15));
        Assert.Equal(88.85m, report.Days[0].Events.Single().Amount);
        Assert.Equal(9911.15m, report.DayView.EndingCash);
        Assert.Equal(schedule.EndingBalance, six.Debts[0].Balance);
        Assert.Equal(88.85m, six.MinimumObligation);
        Assert.Equal(0, six.UnknownMinimumCount);
        Assert.Null(six.Debts[0].PaidOffOn);
        Assert.Equal(0m, twelve.Debts[0].Balance);
        Assert.Equal(0m, twelve.MinimumObligation);
        Assert.Equal(new DateOnly(2026, 12, 15), twelve.Debts[0].PaidOffOn);
        Assert.Equal(DebtScheduleStop.PaidOff, twelve.Debts[0].Stop);
        Assert.Contains(
            report.Milestones,
            item => item.Kind == ForecastMilestoneKind.DebtPaidOff
                && item.Date == new DateOnly(2026, 12, 15)
                && item.Amount == 88.85m
                && item.SourceId == LoanId);
        Assert.Contains(report.Assumptions, line => line.Contains("simple interest", StringComparison.Ordinal));
        Assert.Contains(report.Assumptions, line => line.Contains("not rolled", StringComparison.Ordinal));
        Assert.Equal(ForecastIncomeBasis.Typical, report.IncomeBasis);
    }

    [Fact]
    public void Project_LeavesAMissingRateUnpaidAndUnknown()
    {
        var report = CashForecast.Project(Input(
            cash: 500m,
            debts: [Loan(100m, null, 10m, due: new DateOnly(2026, 1, 15))]));

        var horizon = report.Horizons[0];
        var debt = Assert.Single(horizon.Debts);
        Assert.Equal(500m, horizon.Window.EndingCash);
        Assert.Equal(100m, debt.Balance);
        Assert.Null(debt.MonthlyMinimum);
        Assert.Equal(DebtScheduleStop.RateUnknown, debt.Stop);
        Assert.Null(horizon.MinimumObligation);
        Assert.Equal(1, horizon.UnknownMinimumCount);
        Assert.DoesNotContain(report.Milestones, item => item.Kind == ForecastMilestoneKind.DebtPaidOff);
    }

    [Fact]
    public void Project_StopsWhenTheRateAfterAPromotionIsUnknown()
    {
        var report = CashForecast.Project(Input(
            cash: 500m,
            debts:
            [
                new DebtAmortizationInput(
                    LoanId,
                    "Card",
                    "USD",
                    DebtKind.Revolving,
                    100m,
                    null,
                    0m,
                    new DateOnly(2026, 2, 15),
                    10m,
                    null,
                    new DateOnly(2026, 1, 15),
                    0m)
            ]));

        var debt = report.Horizons[0].Debts[0];
        Assert.Equal(480m, report.Horizons[0].Window.EndingCash);
        Assert.Equal(80m, debt.Balance);
        Assert.Null(debt.MonthlyMinimum);
        Assert.Equal(DebtScheduleStop.RateUnknown, debt.Stop);
        Assert.Null(report.Horizons[0].MinimumObligation);
    }

    [Fact]
    public void Project_StopsADebtThatDoesNotPayDown()
    {
        var report = CashForecast.Project(Input(
            cash: 500m,
            debts: [Loan(100m, 24m, 1m, due: new DateOnly(2026, 1, 15))]));

        var debt = report.Horizons[0].Debts[0];
        Assert.Equal(499m, report.Horizons[0].Window.EndingCash);
        Assert.Equal(101m, debt.Balance);
        Assert.Equal(1m, debt.MonthlyMinimum);
        Assert.Equal(DebtScheduleStop.DoesNotPayDown, debt.Stop);
        Assert.Equal(1m, report.Horizons[0].MinimumObligation);
        Assert.DoesNotContain(report.Milestones, item => item.Kind == ForecastMilestoneKind.DebtPaidOff);
    }

    [Fact]
    public void Project_LeavesAnotherCurrencyOut()
    {
        var report = CashForecast.Project(Input(
            incomes:
            [
                Pay("Pay", 100m, IncomeCadence.Irregular, new DateOnly(2026, 1, 15)),
                Pay("Side", 50m, IncomeCadence.Irregular, new DateOnly(2026, 1, 15), currency: "CAD", id: RentId)
            ],
            bills: [Bill("Fee", 10m, ObligationCadence.Irregular, new DateOnly(2026, 1, 15), currency: "")]));

        Assert.Equal(90m, report.Days.Single(day => day.Date == new DateOnly(2026, 1, 15)).Cash);
        Assert.Equal(["CAD"], report.ExcludedCurrencies);
        Assert.Contains(report.Assumptions, line => line.Contains("CAD", StringComparison.Ordinal));
    }

    [Fact]
    public void Project_SkipsADebtPaymentBeforeTheStartAndKeepsMonthEnd()
    {
        var report = CashForecast.Project(Input(
            asOf: new DateOnly(2026, 2, 1),
            cash: 100m,
            debts: [Loan(100m, 0m, 25m, due: new DateOnly(2026, 1, 31))]));

        var paid = report.Days.SelectMany(day => day.Events).ToList();
        Assert.Equal([new DateOnly(2026, 2, 28)], paid.Select(item => item.Date));
        Assert.Equal(75m, report.DayView.EndingCash);
        Assert.Equal(0m, report.Horizons[0].Debts[0].Balance);
        Assert.Equal(new DateOnly(2026, 5, 31), report.Horizons[0].Debts[0].PaidOffOn);
    }

    [Fact]
    public void Project_AppliesExtraOnThatDebtOnly()
    {
        var report = CashForecast.Project(Input(
            asOf: new DateOnly(2026, 1, 15),
            cash: 200m,
            debts: [Loan(120m, 0m, 50m, extra: 70m, due: new DateOnly(2026, 1, 15))]));

        Assert.Equal(80m, report.DayView.EndingCash);
        Assert.Equal(0m, report.Horizons[0].Debts[0].Balance);
        Assert.Equal(
            new ForecastMilestone(
                new DateOnly(2026, 1, 15),
                ForecastMilestoneKind.DebtPaidOff,
                LoanId,
                "Loan",
                50m),
            Assert.Single(report.Milestones));
    }

    [Fact]
    public void Project_RecordsTheIncomeBasis()
    {
        var conservative = CashForecast.Project(Input(
            basis: ForecastIncomeBasis.Conservative,
            incomes: [Pay("Pay", 800m, IncomeCadence.Monthly, new DateOnly(2026, 1, 15))]));
        var typical = CashForecast.Project(Input(
            incomes: [Pay("Pay", 1000m, IncomeCadence.Monthly, new DateOnly(2026, 1, 15))]));

        Assert.Equal(ForecastIncomeBasis.Conservative, conservative.IncomeBasis);
        Assert.Equal(800m, conservative.DayView.EndingCash);
        Assert.Equal(1000m, typical.DayView.EndingCash);
        Assert.Contains(
            conservative.Assumptions,
            line => line.Contains("conservative payments", StringComparison.Ordinal));
        Assert.Contains(
            typical.Assumptions,
            line => line.Contains("typical payments", StringComparison.Ordinal));
    }

    [Fact]
    public void Project_RepeatsTheSameResult()
    {
        var input = Input(
            cash: 100m,
            incomes: [Pay("Pay", 1000m, IncomeCadence.Irregular, new DateOnly(2026, 1, 9))],
            bills: [Bill("Rent", 500m, ObligationCadence.Irregular, new DateOnly(2026, 1, 5))]);

        var first = CashForecast.Project(input);
        var again = CashForecast.Project(input);

        Assert.Equal(
            first.Days.Select(day => (day.Date, day.Cash, day.Available, day.Reserve)),
            again.Days.Select(day => (day.Date, day.Cash, day.Available, day.Reserve)));
        Assert.Equal(
            first.Horizons.Select(horizon => (horizon.Months, horizon.Window.EndingCash, horizon.MinimumObligation)),
            again.Horizons.Select(horizon => (horizon.Months, horizon.Window.EndingCash, horizon.MinimumObligation)));
        Assert.Equal(first.Milestones, again.Milestones);
        Assert.Equal(first.Assumptions, again.Assumptions);
    }

    private static CashForecastInput Input(
        decimal cash = 0m,
        decimal reserve = 0m,
        ForecastIncomeBasis basis = ForecastIncomeBasis.Typical,
        DateOnly? asOf = null,
        IReadOnlyList<DatedIncome>? incomes = null,
        IReadOnlyList<DatedBill>? bills = null,
        IReadOnlyList<DebtAmortizationInput>? debts = null,
        IReadOnlyList<CashFlowEvent>? savings = null)
    {
        return new CashForecastInput(
            "USD",
            basis,
            asOf ?? Start,
            cash,
            reserve,
            incomes ?? [],
            bills ?? [],
            debts ?? [],
            savings ?? []);
    }

    private static DatedIncome Pay(
        string name,
        decimal amount,
        IncomeCadence cadence,
        DateOnly next,
        IReadOnlyList<DatedIncomeRaise>? raises = null,
        string currency = "USD",
        Guid? id = null)
    {
        return new DatedIncome(
            id ?? PayId,
            name,
            currency,
            amount,
            cadence,
            next,
            raises ?? []);
    }

    private static DatedBill Bill(
        string name,
        decimal amount,
        ObligationCadence cadence,
        DateOnly next,
        string currency = "USD")
    {
        return new DatedBill(RentId, name, currency, amount, cadence, next);
    }

    private static CashFlowEvent Save(string name, decimal amount, DateOnly date)
    {
        return new CashFlowEvent(date, CashFlowKind.Savings, PayId, name, amount, "USD");
    }

    private static DebtAmortizationInput Loan(
        decimal balance,
        decimal? apr,
        decimal? minimum,
        decimal extra = 0m,
        int? remainingTerm = null,
        DateOnly? due = null)
    {
        return new DebtAmortizationInput(
            LoanId,
            "Loan",
            "USD",
            remainingTerm is null ? DebtKind.Revolving : DebtKind.Installment,
            balance,
            apr,
            null,
            null,
            minimum,
            remainingTerm,
            due ?? new DateOnly(2026, 1, 15),
            extra);
    }
}
