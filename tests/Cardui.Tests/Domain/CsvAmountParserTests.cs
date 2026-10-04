using Cardui.Api.Domain;
using Xunit;

namespace Cardui.Tests.Domain;

public class CsvAmountParserTests
{
    [Theory]
    [InlineData("$1,234.50", 1234.50, false)]
    [InlineData("(12.50)", -12.50, false)]
    [InlineData("-8", -8, false)]
    [InlineData("1.234,56", 1234.56, false)]
    [InlineData("12,50", 12.50, false)]
    [InlineData("12.50 CR", -12.50, true)]
    [InlineData("12.50DR", 12.50, true)]
    public void Parse_ReadsBankAmountText(string text, decimal amount, bool explicitDirection)
    {
        var parsed = CsvAmountParser.Parse(text);

        Assert.NotNull(parsed);
        Assert.Equal(amount, parsed.Value.Amount);
        Assert.Equal(explicitDirection, parsed.Value.ExplicitDirection);
    }

    [Theory]
    [InlineData("")]
    [InlineData("coffee")]
    [InlineData("CR")]
    public void Parse_RejectsTextThatIsNotMoney(string text)
    {
        Assert.Null(CsvAmountParser.Parse(text));
    }
}
