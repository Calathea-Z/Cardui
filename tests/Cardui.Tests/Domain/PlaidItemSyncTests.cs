using Cardui.Api.Domain.Plaid;
using Xunit;

namespace Cardui.Tests.Domain;

public class PlaidItemSyncTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 10, 6, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public void HoldsItem_RecentUnfinishedStart_IsTrue()
    {
        var started = Now.AddMinutes(-5);

        Assert.True(PlaidItemSync.HoldsItem(started, Now.AddDays(-1), Now.AddDays(-2), Now));
    }

    [Fact]
    public void HoldsItem_ClosedOrMissingStart_IsFalse()
    {
        Assert.False(PlaidItemSync.HoldsItem(null, Now, null, Now));
        Assert.False(PlaidItemSync.HoldsItem(Now.AddHours(-2), Now.AddHours(-1), null, Now));
        Assert.False(PlaidItemSync.HoldsItem(Now.AddHours(-2), null, Now.AddHours(-1), Now));
        Assert.False(PlaidItemSync.HoldsItem(Now, Now, Now, Now));
    }

    [Fact]
    public void IsInterrupted_StartOlderThanTheLease_IsTrue()
    {
        var started = Now - PlaidItemSync.Lease;

        Assert.True(PlaidItemSync.IsInterrupted(started, Now.AddDays(-1), null, Now));
        Assert.False(PlaidItemSync.HoldsItem(started, Now.AddDays(-1), null, Now));
    }

    [Fact]
    public void IsInterrupted_RecentOrClosedStart_IsFalse()
    {
        Assert.False(PlaidItemSync.IsInterrupted(null, null, null, Now));
        Assert.False(PlaidItemSync.IsInterrupted(
            Now.AddMinutes(-5),
            Now.AddDays(-1),
            null,
            Now));
        Assert.False(PlaidItemSync.IsInterrupted(
            Now - PlaidItemSync.Lease,
            Now,
            null,
            Now));
    }

    [Fact]
    public void FinishTime_DoesNotPrecedeTheStart()
    {
        var started = Now.AddTicks(1);

        Assert.Equal(started, PlaidItemSync.FinishTime(started, Now));
        Assert.Equal(Now, PlaidItemSync.FinishTime(Now.AddMinutes(-1), Now));
        Assert.Equal(Now, PlaidItemSync.FinishTime(null, Now));
    }
}
