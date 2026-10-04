using Cardui.Api.Dtos.Plaid;

namespace Cardui.Api.Services.Plaid;

public interface IPlaidTransactionPageClient
{
    /// <summary>
    /// Fetches one transactions/sync page, including original descriptions.
    /// The caller supplies the cursor from the previous page.
    /// </summary>
    Task<PlaidTransactionPageDto> GetPageAsync(
        string accessToken,
        string? cursor,
        CancellationToken cancellationToken = default);
}
