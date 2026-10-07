namespace Cardui.Api.Domain.Plaid;

/// <summary>
/// Whether one Plaid transaction was inserted, updated, or skipped.
/// </summary>
internal enum TransactionUpsertResult
{
    Skipped,
    Added,
    Modified
}
