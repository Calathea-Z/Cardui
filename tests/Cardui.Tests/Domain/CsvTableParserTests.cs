using Cardui.Api.Domain;
using Xunit;

namespace Cardui.Tests.Domain;

public class CsvTableParserTests
{
    [Fact]
    public void Parse_KeepsQuotedCommasAndLineNumbers()
    {
        var parsed = CsvTableParser.Parse(
            "Date,Name,Amount\r\n2026-10-01,\"Coffee, Shop\",4.50\r\n\r\n2026-10-02,Tea,3.00\r\n");

        Assert.Null(parsed.Error);
        Assert.Equal(["Date", "Name", "Amount"], parsed.Table!.Headers);
        Assert.Equal(2, parsed.Table.Rows.Count);
        Assert.Equal(2, parsed.Table.Rows[0].LineNumber);
        Assert.Equal("Coffee, Shop", parsed.Table.Rows[0].Cells[1]);
        Assert.Equal(4, parsed.Table.Rows[1].LineNumber);
        Assert.Equal("Tea", parsed.Table.Rows[1].Cells[1]);
    }

    [Fact]
    public void Parse_ReadsAQuotedLineBreakAndADoubledQuote()
    {
        var parsed = CsvTableParser.Parse("Name\n\"Line one\nLine \"\"two\"\"\"\n");

        Assert.Null(parsed.Error);
        var row = Assert.Single(parsed.Table!.Rows);
        Assert.Equal("Line one\nLine \"two\"", row.Cells[0]);
    }

    [Fact]
    public void Parse_StripsAByteOrderMarkAndAnExcelSeparator()
    {
        var parsed = CsvTableParser.Parse("\uFEFFsep=,\nDate,Name\n2026-10-01,Coffee\n");

        Assert.Null(parsed.Error);
        Assert.Equal(["Date", "Name"], parsed.Table!.Headers);
        Assert.Equal("Coffee", Assert.Single(parsed.Table.Rows).Cells[1]);
    }

    [Fact]
    public void Parse_RejectsAnUnmatchedQuote()
    {
        var parsed = CsvTableParser.Parse("Name\n\"Coffee\n");

        Assert.Null(parsed.Table);
        Assert.Equal("The CSV has an unmatched quote.", parsed.Error);
    }

    [Fact]
    public void Parse_RejectsAHeaderOnlyFileAsAnEmptyTable()
    {
        var parsed = CsvTableParser.Parse("Date,Name,Amount\n");

        Assert.Null(parsed.Error);
        Assert.Empty(parsed.Table!.Rows);
    }

    [Fact]
    public void Parse_RejectsTooManyRows()
    {
        var lines = new string[TransactionImportLimits.MaxDataRows + 2];
        lines[0] = "Date,Name";
        for (var index = 1; index < lines.Length; index++)
        {
            lines[index] = "2026-10-01,Coffee";
        }

        var parsed = CsvTableParser.Parse(string.Join('\n', lines));

        Assert.Null(parsed.Table);
        Assert.Contains("2000", parsed.Error);
    }
}
