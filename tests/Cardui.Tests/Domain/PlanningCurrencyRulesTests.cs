using Cardui.Api.Domain;
using Xunit;

namespace Cardui.Tests.Domain;

public class PlanningCurrencyRulesTests
{
    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("  ", true)]
    [InlineData("usd", true)]
    [InlineData("USD", true)]
    [InlineData("CAD", false)]
    public void IsIncluded_TreatsBlankAsThePlanningCurrency(
        string? currency,
        bool included)
    {
        Assert.Equal(included, PlanningCurrencyRules.IsIncluded(currency, "USD"));
    }

    [Theory]
    [InlineData("usd", true, "USD")]
    [InlineData("CAD", true, "CAD")]
    [InlineData("US", false, "")]
    [InlineData("USDD", false, "")]
    [InlineData("12A", false, "")]
    public void TryNormalize_AcceptsAThreeLetterCode(
        string value,
        bool valid,
        string expected)
    {
        var normalized = PlanningCurrencyRules.TryNormalize(value, out var code);

        Assert.Equal(valid, normalized);
        Assert.Equal(expected, code);
    }
}
