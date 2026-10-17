using Cardui.Api.Domain.Planning;
using Xunit;

namespace Cardui.Tests.Domain;

public class PlanningCallRulesTests
{
    [Fact]
    public void Limits_RetryTransientStatusesAndStopAfterThreeAttempts()
    {
        Assert.True(PlanningCallRules.IsTransient(408));
        Assert.True(PlanningCallRules.IsTransient(429));
        Assert.True(PlanningCallRules.IsTransient(500));
        Assert.True(PlanningCallRules.IsTransient(502));
        Assert.True(PlanningCallRules.IsTransient(503));
        Assert.True(PlanningCallRules.IsTransient(504));
        Assert.False(PlanningCallRules.IsTransient(200));
        Assert.False(PlanningCallRules.IsTransient(400));
        Assert.False(PlanningCallRules.IsTransient(401));
        Assert.False(PlanningCallRules.IsTransient(404));
        Assert.True(PlanningCallRules.CanRetry(1));
        Assert.True(PlanningCallRules.CanRetry(2));
        Assert.False(PlanningCallRules.CanRetry(PlanningCallRules.MaxAttempts));
        Assert.Equal(1024, PlanningCallRules.MaxOutputTokens);
        Assert.Equal(24000, PlanningCallRules.MaxInputCharacters);
    }

    [Fact]
    public void RejectLength_AllowsTheCapAndRefusesOneCharacterPastIt()
    {
        var atCap = new string('a', PlanningCallRules.MaxInputCharacters);
        var pastCap = atCap + "b";

        Assert.Null(PlanningCallRules.RejectLength(atCap, "Stay brief."));
        Assert.Null(PlanningCallRules.RejectLength("  " + atCap + "  ", null));
        Assert.Null(PlanningCallRules.RejectLength(null, "  "));
        Assert.Equal(
            PlanningCallRules.InputTooLong,
            PlanningCallRules.RejectLength(pastCap, "Stay brief."));
        Assert.Equal(
            PlanningCallRules.InputTooLong,
            PlanningCallRules.RejectLength("Hello", " " + pastCap));
    }

    [Fact]
    public void RejectToken_RefusesABankTokenAndKeepsOrdinaryAccessWording()
    {
        Assert.Equal(
            PlanningShareRules.TokenRefusal,
            PlanningCallRules.RejectToken("See access-sandbox-secret", "Stay brief."));
        Assert.Equal(
            PlanningShareRules.TokenRefusal,
            PlanningCallRules.RejectToken("What is next?", "dp:v1:protected-token"));
        Assert.Null(PlanningCallRules.RejectToken("Please access the savings account.", "Stay brief."));
    }

    [Fact]
    public void LoggedCode_KeepsAShortCodeAndDropsUnsafeText()
    {
        Assert.Equal("none", PlanningCallRules.LoggedCode("  "));
        Assert.Equal("invalid_api_key", PlanningCallRules.LoggedCode(" invalid_api_key "));
        Assert.Equal("redacted", PlanningCallRules.LoggedCode("salary 99999"));
        Assert.Equal("redacted", PlanningCallRules.LoggedCode("dp:v1:protected-token"));
        Assert.Equal(
            "redacted",
            PlanningCallRules.LoggedCode(new string('a', PlanningCallRules.LoggedCodeMaxLength + 1)));
        Assert.Equal(
            new string('a', PlanningCallRules.LoggedCodeMaxLength),
            PlanningCallRules.LoggedCode(new string('a', PlanningCallRules.LoggedCodeMaxLength)));
    }

    [Fact]
    public void Describe_ReportsTheAttemptAndTokenCountsWithoutTheMessage()
    {
        const string secret = "sk-cardui-test-key salary 99999";

        var retry = PlanningCallRules.DescribeRetry(1, 503, "server_error", "overloaded", null);
        var failure = PlanningCallRules.DescribeFailure(3, 429, "salary 99999", "dp:v1:protected-token", null);
        var answered = PlanningCallRules.DescribeAnswer(2, 11, 4);
        var missing = PlanningCallRules.DescribeAnswer(1, null, null);
        var fallback = PlanningCallRules.DescribeFallback(PlanningCallFallback.NoAnswer);

        Assert.Contains("will retry after attempt 1", retry, StringComparison.Ordinal);
        Assert.Contains("Status 503. server_error/overloaded. none", retry, StringComparison.Ordinal);
        Assert.Contains("failed on attempt 3", failure, StringComparison.Ordinal);
        Assert.Contains("redacted/redacted", failure, StringComparison.Ordinal);
        Assert.DoesNotContain(secret, failure, StringComparison.Ordinal);
        Assert.DoesNotContain("dp:v1:", failure, StringComparison.Ordinal);
        Assert.Equal(
            "Planning model answered on attempt 2. Input tokens 11. Output tokens 4.",
            answered);
        Assert.Equal("Planning model answered on attempt 1. Usage was not reported.", missing);
        Assert.Equal(
            "Planning model call fell back to manual planning. Reason NoAnswer.",
            fallback);
        Assert.DoesNotContain(secret, answered, StringComparison.Ordinal);
    }
}
