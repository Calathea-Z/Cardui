namespace Cardui.Api.Dtos.TransactionImport;

public class TransactionImportPreviewRowDto
{
    public int LineNumber { get; set; }

    public DateOnly? Date { get; set; }

    public string? Name { get; set; }

    public decimal? Amount { get; set; }

    public string? CategoryName { get; set; }

    public required string Status { get; set; }

    public string? Message { get; set; }
}
