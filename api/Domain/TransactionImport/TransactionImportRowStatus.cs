namespace Cardui.Api.Domain.TransactionImport;

/// <summary>
/// Whether a preview row can be saved.
/// Duplicate rows match a saved transaction or an earlier row. Error rows are skipped.
/// </summary>
public enum TransactionImportRowStatus
{
    Ready,
    Duplicate,
    Error
}
