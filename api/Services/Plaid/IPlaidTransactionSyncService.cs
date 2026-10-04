using Cardui.Api.Dtos.Plaid;
using Cardui.Api.Models;

namespace Cardui.Api.Services.Plaid;

public interface IPlaidTransactionSyncService
{
    /// <summary>
    /// Reads every transactions/sync page for the item, reconciles added,
    /// modified, and removed rows, then categorizes uncategorized transactions
    /// and pairs transfers. The cursor advances only after reconciliation saves.
    /// </summary>
    Task<SyncTransactionsResponseDto> SyncTransactionsForPlaidItemAsync(
        PlaidItem plaidItem,
        CancellationToken cancellationToken = default);
}
