namespace Cardui.Api.Domain.TransactionImport;

public sealed record TransactionImportDraft(
    int LineNumber,
    DateOnly? Date,
    string? Name,
    decimal? Amount,
    Guid? CategoryId,
    string? CategoryName,
    bool CategoryFromFile,
    string? Notes,
    TransactionImportRowStatus Status,
    string? Message);
