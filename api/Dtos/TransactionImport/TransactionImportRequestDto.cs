using Cardui.Api.Domain.TransactionImport;

namespace Cardui.Api.Dtos.TransactionImport;

public class TransactionImportRequestDto
{
    public Guid AccountId { get; set; }

    public int DateColumn { get; set; }

    public int NameColumn { get; set; }

    public int? AmountColumn { get; set; }

    public int? DebitColumn { get; set; }

    public int? CreditColumn { get; set; }

    public int? CategoryColumn { get; set; }

    public int? NotesColumn { get; set; }

    public CsvAmountSign AmountSign { get; set; } = CsvAmountSign.PositiveOut;

    public CsvDateOrder DateOrder { get; set; } = CsvDateOrder.MonthFirst;

    public string? IncludedLineNumbers { get; set; }
}
