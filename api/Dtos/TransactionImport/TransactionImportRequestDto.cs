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

    public string AmountSign { get; set; } = "";

    public string DateOrder { get; set; } = "";

    public string? IncludedLineNumbers { get; set; }
}
