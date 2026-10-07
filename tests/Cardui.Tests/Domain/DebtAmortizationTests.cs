using Cardui.Api.Domain.Recovery;
using Cardui.Api.Models;
using Xunit;

namespace Cardui.Tests.Domain;

public class DebtAmortizationTests
{
    private static readonly Guid DebtId = Guid.Parse("20000000-0000-0000-0000-000000000001");
    private static readonly DateOnly Due = new(2026, 1, 15);

    [Fact]
    public void Project_PaysTheRemainderOnTheFinalPayment()
    {
        var schedule = DebtAmortization.Project(Loan(120m, 0m, 50m));

        Assert.Equal(DebtScheduleStop.PaidOff, schedule.Stop);
        Assert.Equal([50m, 50m, 20m], schedule.Periods.Select(period => period.Payment));
        Assert.Equal(0m, schedule.EndingBalance);
        Assert.Equal(new DateOnly(2026, 3, 15), schedule.Periods[^1].DueDate);
    }

    [Fact]
    public void Project_ChargesOneMonthOfSimpleInterestThenPrincipal()
    {
        var schedule = DebtAmortization.Project(Loan(100m, 12m, 10m));

        var first = schedule.Periods[0];
        Assert.Equal(1.00m, first.Interest);
        Assert.Equal(10m, first.Payment);
        Assert.Equal(9m, first.Principal);
        Assert.Equal(91m, first.EndingBalance);
        Assert.Equal(0.91m, schedule.Periods[1].Interest);
        Assert.Equal(81.91m, schedule.Periods[1].EndingBalance);
    }

    [Fact]
    public void Project_PaysOffACalculatedTwelveMonthLoan()
    {
        var schedule = DebtAmortization.Project(new DebtAmortizationInput(
            DebtId,
            "Loan",
            "USD",
            DebtKind.Installment,
            1000m,
            12m,
            null,
            null,
            null,
            12,
            Due,
            0m));

        Assert.Equal(DebtScheduleStop.PaidOff, schedule.Stop);
        Assert.Equal(12, schedule.Periods.Count);
        Assert.Equal(88.85m, schedule.Periods[0].Payment);
        Assert.Equal(10.00m, schedule.Periods[0].Interest);
        Assert.Equal(88.84m, schedule.Periods[^1].Payment);
        Assert.Equal(1000m, schedule.Periods.Sum(period => period.Principal));
        Assert.Equal(66.19m, schedule.Periods.Sum(period => period.Interest));
        Assert.Equal(0m, schedule.EndingBalance);

        var again = DebtAmortization.Project(new DebtAmortizationInput(
            DebtId,
            "Loan",
            "USD",
            DebtKind.Installment,
            1000m,
            12m,
            null,
            null,
            null,
            12,
            Due,
            0m));
        Assert.Equal(schedule.Periods, again.Periods);
    }

    [Fact]
    public void Project_ClearsARoundedCentInALaterPeriod()
    {
        var schedule = DebtAmortization.Project(new DebtAmortizationInput(
            DebtId,
            "Loan",
            "USD",
            DebtKind.Installment,
            100m,
            0m,
            null,
            null,
            null,
            3,
            Due,
            0m));

        Assert.Equal(DebtScheduleStop.PaidOff, schedule.Stop);
        Assert.Equal([33.33m, 33.33m, 33.33m, 0.01m], schedule.Periods.Select(period => period.Payment));
    }

    [Fact]
    public void Project_StopsWhenThePaymentDoesNotReduceTheBalance()
    {
        var schedule = DebtAmortization.Project(Loan(100m, 24m, 1m));

        var period = Assert.Single(schedule.Periods);
        Assert.Equal(DebtScheduleStop.DoesNotPayDown, schedule.Stop);
        Assert.Equal(2.00m, period.Interest);
        Assert.Equal(1m, period.Payment);
        Assert.Equal(101m, period.EndingBalance);
        Assert.Equal(101m, schedule.EndingBalance);
    }

    [Fact]
    public void Project_UsesThePromotionalRateThroughItsEndDate()
    {
        var schedule = DebtAmortization.Project(new DebtAmortizationInput(
            DebtId,
            "Card",
            "USD",
            DebtKind.Revolving,
            100m,
            24m,
            0m,
            new DateOnly(2026, 2, 15),
            10m,
            null,
            Due,
            0m));

        Assert.Equal(0m, schedule.Periods[0].Interest);
        Assert.True(schedule.Periods[0].RateIsPromotional);
        Assert.Equal(0m, schedule.Periods[1].Interest);
        Assert.Equal(new DateOnly(2026, 2, 15), schedule.Periods[1].DueDate);
        Assert.Equal(1.60m, schedule.Periods[2].Interest);
        Assert.Equal(24m, schedule.Periods[2].RatePercent);
        Assert.False(schedule.Periods[2].RateIsPromotional);
        Assert.Equal(71.60m, schedule.Periods[2].EndingBalance);
    }

    [Fact]
    public void Project_StopsWhenTheRateAfterAPromotionIsUnknown()
    {
        var schedule = DebtAmortization.Project(new DebtAmortizationInput(
            DebtId,
            "Card",
            "USD",
            DebtKind.Revolving,
            100m,
            null,
            0m,
            new DateOnly(2026, 2, 15),
            10m,
            null,
            Due,
            0m));

        Assert.Equal(2, schedule.Periods.Count);
        Assert.Equal(DebtScheduleStop.RateUnknown, schedule.Stop);
        Assert.Equal(80m, schedule.EndingBalance);
    }

    [Fact]
    public void Project_AppliesExtraUntilThisDebtIsPaidOff()
    {
        var schedule = DebtAmortization.Project(Loan(120m, 0m, 50m, extra: 70m));

        var period = Assert.Single(schedule.Periods);
        Assert.Equal(DebtScheduleStop.PaidOff, schedule.Stop);
        Assert.Equal(50m, period.MinimumPaid);
        Assert.Equal(70m, period.ExtraPaid);
        Assert.Equal(120m, period.Payment);
    }

    [Fact]
    public void Project_StopsWhenTheRateOrDueDateOrMinimumIsUnknown()
    {
        var missingRate = DebtAmortization.Project(Loan(100m, null, 10m));
        var missingDate = DebtAmortization.Project(new DebtAmortizationInput(
            DebtId,
            "Card",
            "USD",
            DebtKind.Revolving,
            100m,
            12m,
            null,
            null,
            10m,
            null,
            null,
            0m));
        var missingMinimum = DebtAmortization.Project(new DebtAmortizationInput(
            DebtId,
            "Card",
            "USD",
            DebtKind.Revolving,
            100m,
            12m,
            null,
            null,
            null,
            null,
            Due,
            0m));

        Assert.Equal(DebtScheduleStop.RateUnknown, missingRate.Stop);
        Assert.Equal(100m, missingRate.EndingBalance);
        Assert.Equal(DebtScheduleStop.DueDateUnknown, missingDate.Stop);
        Assert.Equal(DebtScheduleStop.MinimumUnknown, missingMinimum.Stop);
        Assert.Empty(missingRate.Periods);
        Assert.Empty(missingDate.Periods);
        Assert.Empty(missingMinimum.Periods);
    }

    [Fact]
    public void Project_TreatsAZeroBalanceAsPaidOff()
    {
        var schedule = DebtAmortization.Project(Loan(0m, 12m, 10m));

        Assert.Equal(DebtScheduleStop.PaidOff, schedule.Stop);
        Assert.Equal(0m, schedule.EndingBalance);
        Assert.Empty(schedule.Periods);
    }

    [Fact]
    public void Project_StopsAtTheHorizonBeforeTheFirstDueDate()
    {
        var schedule = DebtAmortization.Project(
            Loan(100m, 0m, 25m, due: new DateOnly(2026, 3, 1)),
            new DateOnly(2026, 2, 28));

        Assert.Equal(DebtScheduleStop.HorizonReached, schedule.Stop);
        Assert.Empty(schedule.Periods);
        Assert.Equal(100m, schedule.EndingBalance);
    }

    [Fact]
    public void Project_SkipsPaymentsBeforeTheStartAndKeepsTheMonthEndDay()
    {
        var schedule = DebtAmortization.Project(
            Loan(100m, 0m, 25m, due: new DateOnly(2026, 1, 31)),
            new DateOnly(2026, 5, 31),
            new DateOnly(2026, 2, 1));

        Assert.Equal(
            [
                new DateOnly(2026, 2, 28),
                new DateOnly(2026, 3, 31),
                new DateOnly(2026, 4, 30),
                new DateOnly(2026, 5, 31)
            ],
            schedule.Periods.Select(period => period.DueDate));
        Assert.Equal(DebtScheduleStop.PaidOff, schedule.Stop);
        Assert.Equal(0m, schedule.EndingBalance);
        Assert.Equal(100m, schedule.Periods.Sum(period => period.Payment));
    }

    [Fact]
    public void Project_KeepsAMonthEndDueDateAfterFebruary()
    {
        var schedule = DebtAmortization.Project(
            Loan(30m, 0m, 10m, due: new DateOnly(2026, 1, 31)));

        Assert.Equal(
            [
                new DateOnly(2026, 1, 31),
                new DateOnly(2026, 2, 28),
                new DateOnly(2026, 3, 31)
            ],
            schedule.Periods.Select(period => period.DueDate));
    }

    [Fact]
    public void Project_StopsAfterSixHundredMonthsWhileABalanceRemains()
    {
        var schedule = DebtAmortization.Project(Loan(600m, 0m, 0.50m));

        Assert.Equal(600, schedule.Periods.Count);
        Assert.Equal(DebtScheduleStop.HorizonReached, schedule.Stop);
        Assert.Equal(300m, schedule.EndingBalance);
    }

    private static DebtAmortizationInput Loan(
        decimal balance,
        decimal? apr,
        decimal? minimum,
        decimal extra = 0m,
        DateOnly? due = null)
    {
        return new DebtAmortizationInput(
            DebtId,
            "Card",
            "USD",
            DebtKind.Revolving,
            balance,
            apr,
            null,
            null,
            minimum,
            null,
            due ?? Due,
            extra);
    }
}
