namespace Cardui.Api.Dtos.TransactionImport;

public class TransactionImportInspectDto
{
    public required string FileName { get; set; }

    public required IReadOnlyList<string> Headers { get; set; }

    public required IReadOnlyList<IReadOnlyList<string>> SampleRows { get; set; }

    public int DataRowCount { get; set; }

    public required TransactionImportSuggestedMapDto Suggested { get; set; }
}
