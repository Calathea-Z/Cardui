using Cardui.Api.Dtos.Plaid;
using Cardui.Api.Models;

namespace Cardui.Api.Services.Plaid;

public interface IPlaidTransactionSyncService
{
    Task<SyncTransactionsResponseDto> SyncTransactionsForPlaidItemAsync(
        PlaidItem plaidItem,
        CancellationToken cancellationToken = default);
}
