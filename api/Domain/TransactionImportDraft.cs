namespace Cardui.Api.Domain;

public sealed record TransactionImportDraft(
    int LineNumber,
    DateOnly? Date,
    string? Name,
    decimal? Amount,
    Guid? CategoryId,
    string? CategoryName,
    bool CategoryFromFile,
    string? Notes,
    string Status,
    string? Message);
