namespace Cardui.Api.Dtos.TransactionImport;

public class TransactionImportBatchDto
{
    public Guid Id { get; set; }

    public Guid AccountId { get; set; }

    public required string AccountName { get; set; }

    public required string FileName { get; set; }

    public int ImportedCount { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UndoneAt { get; set; }
}
