using Cardui.Api.Models;

namespace Cardui.Api.Services.Plaid;

public interface IPlaidAccountSyncService
{
    Task SyncAccountsForPlaidItemAsync(
        PlaidItem plaidItem,
        CancellationToken cancellationToken = default);
}
