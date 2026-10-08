using Cardui.Api.Domain.Recovery;
using Cardui.Api.Domain.Savings;
using Cardui.Api.Models;
using Xunit;

namespace Cardui.Tests.Domain;

public class HouseholdSavingsTests
{
    private static readonly DateOnly Today = new(2026, 1, 1);
    private static readonly Guid EmergencyId = Guid.Parse("80000000-0000-0000-0000-000000000001");
    private static readonly Guid TripId = Guid.Parse("80000000-0000-0000-0000-000000000002");

    [Fact]
    public void Project_ReservesWhatIsSetAsideAndSchedulesTheGap()
    {
        var outlook = HouseholdSavings.Project(
            Today,
            "USD",
            [Goal(EmergencyId, "Emergency", "USD", 100m, new DateOnly(2026, 4, 1), 40m)]);

        Assert.Equal(40m, outlook.StartingReserve);
        Assert.Equal(
            [
                new SavingsContribution(new DateOnly(2026, 1, 1), 15m),
                new SavingsContribution(new DateOnly(2026, 2, 1), 15m),
                new SavingsContribution(new DateOnly(2026, 3, 1), 15m),
                new SavingsContribution(new DateOnly(2026, 4, 1), 15m)
            ],
            outlook.Contributions.Select(item => new SavingsContribution(item.Date, item.Amount)));
        Assert.All(outlook.Contributions, item => Assert.Equal(CashFlowKind.Savings, item.Kind));
    }

    [Fact]
    public void Project_LeavesAnotherCurrencyOutOfTheReserveAndStillListsIt()
    {
        var outlook = HouseholdSavings.Project(
            Today,
            "USD",
            [
                Goal(EmergencyId, "Emergency", "USD", 50m, Today, 50m),
                Goal(TripId, "Trip", "CAD", 20m, Today, 10m)
            ]);

        Assert.Equal(50m, outlook.StartingReserve);
        var trip = Assert.Single(outlook.Contributions);
        Assert.Equal("CAD", trip.Currency);
        Assert.Equal(10m, trip.Amount);
    }

    [Fact]
    public void Project_LivingSpendingLeavesCashAndTheFloorStaysProtected()
    {
        var spendingId = Guid.Parse("80000000-0000-0000-0000-000000000003");
        var floorId = Guid.Parse("80000000-0000-0000-0000-000000000004");
        var outlook = HouseholdSavings.Project(
            new DateOnly(2026, 10, 8),
            "USD",
            [
                new SavingsGoalSnapshot(
                    spendingId,
                    "Monthly living spending",
                    "USD",
                    0m,
                    default,
                    300m,
                    SavingsGoalKind.Operating,
                    800m,
                    1,
                    0m),
                new SavingsGoalSnapshot(
                    floorId,
                    "Cash to keep",
                    "USD",
                    0m,
                    default,
                    300m,
                    SavingsGoalKind.Floor,
                    0m,
                    1,
                    800m)
            ]);

        Assert.Equal(800m, outlook.StartingReserve);
        Assert.Equal(800m, outlook.LivingSpendingMonthly);
        Assert.Contains(outlook.Contributions, item =>
            item.Kind == CashFlowKind.LivingSpending && item.Date == new DateOnly(2026, 10, 8) && item.Amount == 300m);
        Assert.DoesNotContain(outlook.Contributions, item => item.Kind == CashFlowKind.Savings);
    }

    private static SavingsGoalSnapshot Goal(
        Guid id,
        string name,
        string currency,
        decimal target,
        DateOnly date,
        decimal reserved)
    {
        return new SavingsGoalSnapshot(id, name, currency, target, date, reserved);
    }
}
