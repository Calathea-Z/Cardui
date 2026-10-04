namespace Cardui.Api.Dtos.TransactionImport;

public class TransactionImportPreviewDto
{
    public int ReadyCount { get; set; }

    public int DuplicateCount { get; set; }

    public int ErrorCount { get; set; }

    public required IReadOnlyList<TransactionImportPreviewRowDto> Rows { get; set; }
}
