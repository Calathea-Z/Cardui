using Cardui.Api.Dtos.TransactionImport;

namespace Cardui.Api.Services.Interfaces;

public interface ITransactionImportService
{
    /// <summary>
    /// Reads the CSV header, a few sample rows, and a suggested column map.
    /// Nothing is saved.
    /// </summary>
    Task<TransactionImportInspectDto> InspectAsync(
        Stream content,
        string? fileName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Parses the CSV with the chosen columns and marks duplicates against
    /// the account. Nothing is saved.
    /// </summary>
    Task<TransactionImportPreviewDto> PreviewAsync(
        Stream content,
        string? fileName,
        TransactionImportRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports the selected file lines onto the account and stores the batch
    /// so it can be undone. Error lines are rejected.
    /// </summary>
    Task<TransactionImportBatchDto> CommitAsync(
        Stream content,
        string? fileName,
        TransactionImportRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists the household's imports that have not been undone, newest first.
    /// </summary>
    Task<IReadOnlyList<TransactionImportBatchDto>> ListOpenAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Archives every transaction in the batch and recalculates a manual balance.
    /// </summary>
    Task<TransactionImportBatchDto> UndoAsync(
        Guid importId,
        CancellationToken cancellationToken = default);
}
