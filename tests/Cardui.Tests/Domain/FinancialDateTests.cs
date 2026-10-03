using Cardui.Api.Domain;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Cardui.Tests.Domain;

public class FinancialDateTests
{
    [Fact]
    public void Today_UsesTheConfiguredLocalTimeZone()
    {
        var timeProvider = new FakeTimeProvider(
            new DateTimeOffset(2026, 10, 3, 4, 38, 0, TimeSpan.Zero));
        timeProvider.SetLocalTimeZone(
            TimeZoneInfo.CreateCustomTimeZone(
                "UTC-06",
                TimeSpan.FromHours(-6),
                "UTC-06",
                "UTC-06"));

        var today = FinancialDate.Today(timeProvider);

        Assert.Equal(new DateOnly(2026, 10, 2), today);
    }
}
