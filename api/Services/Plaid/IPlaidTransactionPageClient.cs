using Cardui.Api.Dtos.Plaid;

namespace Cardui.Api.Services.Plaid;

public interface IPlaidTransactionPageClient
{
    Task<PlaidTransactionPageDto> GetPageAsync(
        string accessToken,
        string? cursor,
        CancellationToken cancellationToken = default);
}
