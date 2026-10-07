using Cardui.Api.Domain.Income;
using Cardui.Api.Models;
using Xunit;

namespace Cardui.Tests.Domain;

public class PaycheckScheduleTests
{
    [Fact]
    public void UpcomingDates_KeepsThreeBiweeklyPaychecksInJanuary()
    {
        var dates = PaycheckSchedule.UpcomingDates(
            IncomeCadence.Biweekly,
            new DateOnly(2026, 1, 2),
            new DateOnly(2026, 1, 1));

        Assert.Equal(
            [
                new DateOnly(2026, 1, 2),
                new DateOnly(2026, 1, 16),
                new DateOnly(2026, 1, 30),
                new DateOnly(2026, 2, 13),
                new DateOnly(2026, 2, 27),
                new DateOnly(2026, 3, 13),
                new DateOnly(2026, 3, 27)
            ],
            dates);
        Assert.Equal(3, dates.Count(date => date.Month == 1));
        Assert.Equal(2166.67m, PaycheckSchedule.AverageMonthlyAmount(1000m, IncomeCadence.Biweekly));
        Assert.DoesNotContain(dates, date => date == new DateOnly(2026, 1, 1));
    }

    [Fact]
    public void UpcomingDates_SkipsPastBiweeklyDatesWithoutMovingTheWeekday()
    {
        var dates = PaycheckSchedule.UpcomingDates(
            IncomeCadence.Biweekly,
            new DateOnly(2026, 1, 2),
            new DateOnly(2026, 2, 1));

        Assert.Equal(new DateOnly(2026, 2, 13), dates[0]);
        Assert.DoesNotContain(new DateOnly(2026, 2, 1), dates);
    }

    [Fact]
    public void UpcomingDates_RestoresAMonthEndDayAfterFebruary()
    {
        var dates = PaycheckSchedule.UpcomingDates(
            IncomeCadence.Monthly,
            new DateOnly(2026, 1, 31),
            new DateOnly(2026, 1, 31));

        Assert.Equal(
            [
                new DateOnly(2026, 1, 31),
                new DateOnly(2026, 2, 28),
                new DateOnly(2026, 3, 31)
            ],
            dates);
    }

    [Fact]
    public void UpcomingDates_KeepsSemimonthlyOnTwoDays()
    {
        var dates = PaycheckSchedule.UpcomingDates(
            IncomeCadence.Semimonthly,
            new DateOnly(2026, 10, 15),
            new DateOnly(2026, 10, 1));

        Assert.Equal(
            [
                new DateOnly(2026, 10, 15),
                new DateOnly(2026, 10, 30),
                new DateOnly(2026, 11, 15),
                new DateOnly(2026, 11, 30),
                new DateOnly(2026, 12, 15),
                new DateOnly(2026, 12, 30)
            ],
            dates);
        Assert.DoesNotContain(new DateOnly(2026, 10, 29), dates);
    }

    [Fact]
    public void UpcomingDates_UsesTheLastDayWhenASemimonthlyDayIsMissing()
    {
        var dates = PaycheckSchedule.UpcomingDates(
            IncomeCadence.Semimonthly,
            new DateOnly(2026, 1, 31),
            new DateOnly(2026, 1, 1));

        Assert.Contains(new DateOnly(2026, 1, 31), dates);
        Assert.Contains(new DateOnly(2026, 2, 16), dates);
        Assert.Contains(new DateOnly(2026, 2, 28), dates);
        Assert.Contains(new DateOnly(2026, 3, 16), dates);
        Assert.Contains(new DateOnly(2026, 3, 31), dates);
    }

    [Fact]
    public void UpcomingDates_StepsWeeklyBySevenDays()
    {
        var dates = PaycheckSchedule.UpcomingDates(
            IncomeCadence.Weekly,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 1));

        Assert.Equal(new DateOnly(2026, 1, 1), dates[0]);
        Assert.Equal(new DateOnly(2026, 1, 8), dates[1]);
        Assert.Equal(new DateOnly(2026, 1, 29), dates[4]);
    }

    [Fact]
    public void UpcomingDates_KeepsAQuarterlyDateOnTheAnchorDay()
    {
        var dates = PaycheckSchedule.UpcomingDates(
            IncomeCadence.Quarterly,
            new DateOnly(2026, 1, 31),
            new DateOnly(2026, 4, 1));

        var date = Assert.Single(dates);
        Assert.Equal(new DateOnly(2026, 4, 30), date);
    }

    [Fact]
    public void UpcomingDates_ClampsALeapDayYearToFebruary28()
    {
        var dates = PaycheckSchedule.UpcomingDates(
            IncomeCadence.Yearly,
            new DateOnly(2024, 2, 29),
            new DateOnly(2025, 2, 1));

        var date = Assert.Single(dates);
        Assert.Equal(new DateOnly(2025, 2, 28), date);
    }

    [Fact]
    public void UpcomingDates_IsEmptyWhenThereIsNoSchedule()
    {
        var dates = PaycheckSchedule.UpcomingDates(
            IncomeCadence.Irregular,
            new DateOnly(2026, 10, 16),
            new DateOnly(2026, 10, 4));

        Assert.Empty(dates);
        Assert.Null(PaycheckSchedule.AverageMonthlyAmount(2400m, IncomeCadence.Irregular));
    }

    [Fact]
    public void AverageMonthlyAmount_CountsAFullYearOfPayments()
    {
        Assert.Equal(5200m, PaycheckSchedule.AverageMonthlyAmount(1200m, IncomeCadence.Weekly));
        Assert.Equal(2000m, PaycheckSchedule.AverageMonthlyAmount(1000m, IncomeCadence.Semimonthly));
        Assert.Equal(1000m, PaycheckSchedule.AverageMonthlyAmount(1000m, IncomeCadence.Monthly));
        Assert.Equal(1000m, PaycheckSchedule.AverageMonthlyAmount(3000m, IncomeCadence.Quarterly));
        Assert.Equal(1000m, PaycheckSchedule.AverageMonthlyAmount(12000m, IncomeCadence.Yearly));
        Assert.Equal(21.69m, PaycheckSchedule.AverageMonthlyAmount(10.01m, IncomeCadence.Biweekly));
    }
}
