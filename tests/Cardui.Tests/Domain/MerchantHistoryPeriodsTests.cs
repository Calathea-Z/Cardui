using Cardui.Api.Domain;
using Xunit;

namespace Cardui.Tests.Domain;

public class MerchantHistoryPeriodsTests
{
    [Fact]
    public void Build_FillsMissingMonthsAndKeepsTodayWhenItHasNoTransactions()
    {
        var series = MerchantHistoryPeriods.Build(
            new DateOnly(2026, 7, 20),
            "monthly",
            [new MerchantHistoryActivity(new DateOnly(2026, 6, 1), 20m)]);

        Assert.Equal("monthly", series.Granularity);
        Assert.Equal("2026-07", series.SelectedPeriodKey);
        Assert.Equal(["2026-06", "2026-07"], series.Periods.Select(period => period.Key).ToArray());

        var june = series.Periods[0];
        Assert.Equal(20m, june.TotalAmount);
        Assert.Equal(1, june.TransactionCount);

        var july = series.Periods[1];
        Assert.Equal(0m, july.TotalAmount);
        Assert.Equal(0, july.TransactionCount);
    }

    [Fact]
    public void Build_SumsTransactionsThatFallInTheSameMonth()
    {
        var series = MerchantHistoryPeriods.Build(
            new DateOnly(2026, 7, 20),
            "monthly",
            [
                new MerchantHistoryActivity(new DateOnly(2026, 7, 2), 10m),
                new MerchantHistoryActivity(new DateOnly(2026, 7, 18), 5m)
            ]);

        var july = Assert.Single(series.Periods);
        Assert.Equal("2026-07", july.Key);
        Assert.Equal(15m, july.TotalAmount);
        Assert.Equal(2, july.TransactionCount);
    }

    [Fact]
    public void Build_GroupsQuartersAndLabelsThem()
    {
        var series = MerchantHistoryPeriods.Build(
            new DateOnly(2026, 4, 2),
            " quarterly ",
            [
                new MerchantHistoryActivity(new DateOnly(2026, 1, 15), 10m),
                new MerchantHistoryActivity(new DateOnly(2026, 4, 2), 5m)
            ]);

        Assert.Equal("quarterly", series.Granularity);
        Assert.Equal("2026-Q2", series.SelectedPeriodKey);
        Assert.Equal("Q1 2026", series.Periods[0].Label);
        Assert.Equal("Q1", series.Periods[0].ShortLabel);
        Assert.Equal(10m, series.Periods[0].TotalAmount);
        Assert.Equal("Q2 2026", series.Periods[1].Label);
        Assert.Equal(5m, series.Periods[1].TotalAmount);
    }

    [Fact]
    public void Build_GroupsYears()
    {
        var series = MerchantHistoryPeriods.Build(
            new DateOnly(2026, 3, 1),
            "yearly",
            [
                new MerchantHistoryActivity(new DateOnly(2025, 11, 1), 8m),
                new MerchantHistoryActivity(new DateOnly(2026, 1, 4), 3m)
            ]);

        Assert.Equal("yearly", series.Granularity);
        Assert.Equal("2026", series.SelectedPeriodKey);
        Assert.Equal("2025", series.Periods[0].Key);
        Assert.Equal(8m, series.Periods[0].TotalAmount);
        Assert.Equal("2026", series.Periods[1].Label);
        Assert.Equal(3m, series.Periods[1].TotalAmount);
    }

    [Fact]
    public void Build_IncludesTodayWhenThereAreNoTransactions()
    {
        var series = MerchantHistoryPeriods.Build(
            new DateOnly(2026, 7, 20),
            "not-a-granularity",
            []);

        var period = Assert.Single(series.Periods);
        Assert.Equal("monthly", series.Granularity);
        Assert.Equal("2026-07", series.SelectedPeriodKey);
        Assert.Equal("2026-07", period.Key);
        Assert.Equal(0, period.TransactionCount);
    }

    [Fact]
    public void Build_KeepsTodayWhenEveryTransactionIsLater()
    {
        var series = MerchantHistoryPeriods.Build(
            new DateOnly(2026, 6, 1),
            "monthly",
            [new MerchantHistoryActivity(new DateOnly(2026, 8, 15), 12m)]);

        var period = Assert.Single(series.Periods);
        Assert.Equal("2026-06", period.Key);
        Assert.Equal(0m, period.TotalAmount);
        Assert.Equal("2026-06", series.SelectedPeriodKey);
    }
}
