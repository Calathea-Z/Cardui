using Cardui.Api.Domain.Recovery;
using Cardui.Api.Domain.Savings;
using Xunit;

namespace Cardui.Tests.Domain;

public class LivingSpendingScheduleTests
{
    private static readonly Guid GoalId = Guid.Parse("81000000-0000-0000-0000-000000000001");

    [Fact]
    public void Schedule_CountsWhatIsLeftAfterTheReadyDayAndTheFullAmountLater()
    {
        var events = LivingSpendingSchedule.Schedule(
            new DateOnly(2026, 10, 8),
            1,
            800m,
            300m,
            GoalId,
            "Monthly living spending",
            "USD");

        Assert.Equal(CashFlowKind.LivingSpending, events[0].Kind);
        Assert.Equal(new DateOnly(2026, 10, 8), events[0].Date);
        Assert.Equal(300m, events[0].Amount);
        Assert.Equal(new DateOnly(2026, 11, 1), events[1].Date);
        Assert.Equal(800m, events[1].Amount);
        Assert.Equal(new DateOnly(2028, 4, 1), events[^1].Date);
        Assert.DoesNotContain(events, item => item.Date == new DateOnly(2026, 10, 1));
    }

    [Fact]
    public void Schedule_CountsTheFullMonthWhenTheReadyDayIsStillAhead()
    {
        var events = LivingSpendingSchedule.Schedule(
            new DateOnly(2026, 10, 1),
            15,
            800m,
            100m,
            GoalId,
            "Monthly living spending",
            "USD");

        Assert.Equal(new DateOnly(2026, 10, 15), events[0].Date);
        Assert.Equal(800m, events[0].Amount);
    }

    [Fact]
    public void OnDay_UsesTheLastDayOfAShortMonth()
    {
        Assert.Equal(new DateOnly(2026, 2, 28), LivingSpendingSchedule.OnDay(2026, 2, 31));
    }
}
