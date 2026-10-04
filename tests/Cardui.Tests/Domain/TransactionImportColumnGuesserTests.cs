using Cardui.Api.Domain;
using Xunit;

namespace Cardui.Tests.Domain;

public class TransactionImportColumnGuesserTests
{
    [Fact]
    public void Guess_MapsASingleAmountExport()
    {
        var suggested = TransactionImportColumnGuesser.Guess(
            ["Posted Date", "Description", "Amount", "Category", "Memo"]);

        Assert.Equal(0, suggested.DateColumn);
        Assert.Equal(1, suggested.NameColumn);
        Assert.Equal(2, suggested.AmountColumn);
        Assert.Equal(3, suggested.CategoryColumn);
        Assert.Equal(4, suggested.NotesColumn);
        Assert.Null(suggested.DebitColumn);
        Assert.Null(suggested.CreditColumn);
    }

    [Fact]
    public void Guess_MapsDebitAndCreditAndUsesMemoAsTheName()
    {
        var suggested = TransactionImportColumnGuesser.Guess(
            ["Date", "Debit", "Credit", "Memo", "Credit Card"]);

        Assert.Equal(0, suggested.DateColumn);
        Assert.Equal(3, suggested.NameColumn);
        Assert.Equal(1, suggested.DebitColumn);
        Assert.Equal(2, suggested.CreditColumn);
        Assert.Null(suggested.AmountColumn);
        Assert.Null(suggested.NotesColumn);
    }

    [Fact]
    public void Validate_RejectsTwoFieldsOnTheSameColumn()
    {
        var message = TransactionImportColumnMapRules.Validate(
            new TransactionImportColumnMap(
                DateColumn: 0,
                NameColumn: 0,
                AmountColumn: 1,
                DebitColumn: null,
                CreditColumn: null,
                CategoryColumn: null,
                NotesColumn: null,
                CsvAmountSign.PositiveOut,
                CsvDateOrder.MonthFirst),
            columnCount: 2);

        Assert.Equal("Choose a different column for each field.", message);
    }
}
