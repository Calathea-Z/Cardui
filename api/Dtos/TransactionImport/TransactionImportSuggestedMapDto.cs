namespace Cardui.Api.Dtos.TransactionImport;

public class TransactionImportSuggestedMapDto
{
    public int? DateColumn { get; set; }

    public int? NameColumn { get; set; }

    public int? AmountColumn { get; set; }

    public int? DebitColumn { get; set; }

    public int? CreditColumn { get; set; }

    public int? CategoryColumn { get; set; }

    public int? NotesColumn { get; set; }

    public required string AmountSign { get; set; }

    public required string DateOrder { get; set; }
}
