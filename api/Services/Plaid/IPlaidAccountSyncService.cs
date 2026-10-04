using Cardui.Api.Models;

namespace Cardui.Api.Services.Plaid;

public interface IPlaidAccountSyncService
{
    /// <summary>
    /// Upserts the item's Plaid accounts, deactivates accounts Plaid no
    /// longer returns, and replaces today's balance snapshot for each
    /// account that was updated.
    /// </summary>
    Task SyncAccountsForPlaidItemAsync(
        PlaidItem plaidItem,
        CancellationToken cancellationToken = default);
}
