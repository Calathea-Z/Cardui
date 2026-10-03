using Cardui.Api.Domain;
using Xunit;

namespace Cardui.Tests.Domain;

public class AccountBalanceHistoryTests
{
    private static readonly Guid CheckingId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid CardId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public void Build_CarriesLastKnownBalanceWhenALaterDayOmitsAnAccount()
    {
        var points = AccountBalanceHistory.Build(
        [
            Snapshot(CheckingId, AccountTypes.Depository, new DateOnly(2026, 10, 1), 1_000m),
            Snapshot(CardId, AccountTypes.Credit, new DateOnly(2026, 10, 1), 400m),
            Snapshot(CheckingId, AccountTypes.Depository, new DateOnly(2026, 10, 3), 900m)
        ]);

        Assert.Equal(2, points.Count);
        Assert.Equal(600m, points[0].NetWorth);
        Assert.Equal(900m, points[1].Cash);
        Assert.Equal(400m, points[1].CreditCards);
        Assert.Equal(500m, points[1].NetWorth);
    }

    [Fact]
    public void Build_DoesNotIncludeAnAccountBeforeItsFirstSnapshot()
    {
        var points = AccountBalanceHistory.Build(
        [
            Snapshot(CheckingId, AccountTypes.Depository, new DateOnly(2026, 10, 1), 1_000m),
            Snapshot(CardId, AccountTypes.Credit, new DateOnly(2026, 10, 2), 250m)
        ]);

        Assert.Equal(1_000m, points[0].NetWorth);
        Assert.Equal(0m, points[0].CreditCards);
        Assert.Equal(750m, points[1].NetWorth);
    }

    [Fact]
    public void Build_UsesTheLatestSnapshotWhenAnAccountIsRecordedTwiceOnOneDay()
    {
        var day = new DateOnly(2026, 10, 1);
        var points = AccountBalanceHistory.Build(
        [
            Snapshot(CheckingId, AccountTypes.Depository, day, 100m, hour: 8),
            Snapshot(CheckingId, AccountTypes.Depository, day, 180m, hour: 18)
        ]);

        var point = Assert.Single(points);
        Assert.Equal(180m, point.Cash);
        Assert.Equal(180m, point.NetWorth);
    }

    [Fact]
    public void Build_ReturnsNoPointsWhenThereAreNoSnapshots()
    {
        Assert.Empty(AccountBalanceHistory.Build([]));
    }

    private static AccountSnapshotBalance Snapshot(
        Guid accountId,
        string type,
        DateOnly date,
        decimal balance,
        int hour = 12) =>
        new(
            accountId,
            type,
            date,
            balance,
            new DateTimeOffset(date, new TimeOnly(hour, 0), TimeSpan.Zero));
}
