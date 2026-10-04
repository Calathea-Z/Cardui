using Cardui.Api.Domain;
using Xunit;

namespace Cardui.Tests.Domain;

public class CsvDateParserTests
{
    [Theory]
    [InlineData("2026-10-04", CsvDateOrder.MonthFirst, 2026, 10, 4)]
    [InlineData("2026-10-04T13:00:00", CsvDateOrder.MonthFirst, 2026, 10, 4)]
    [InlineData("10/04/2026", CsvDateOrder.MonthFirst, 2026, 10, 4)]
    [InlineData("04/10/2026", CsvDateOrder.DayFirst, 2026, 10, 4)]
    [InlineData("10/04/26", CsvDateOrder.MonthFirst, 2026, 10, 4)]
    [InlineData("13/01/2026", CsvDateOrder.MonthFirst, 2026, 1, 13)]
    [InlineData("Oct 4, 2026", CsvDateOrder.DayFirst, 2026, 10, 4)]
    public void Parse_ReadsCommonBankDates(
        string text,
        CsvDateOrder dateOrder,
        int year,
        int month,
        int day)
    {
        Assert.Equal(new DateOnly(year, month, day), CsvDateParser.Parse(text, dateOrder));
    }

    [Theory]
    [InlineData("not-a-date")]
    [InlineData("2026-13-01")]
    [InlineData("02/30/2026")]
    public void Parse_RejectsAnImpossibleDate(string text)
    {
        Assert.Null(CsvDateParser.Parse(text, CsvDateOrder.MonthFirst));
    }
}
