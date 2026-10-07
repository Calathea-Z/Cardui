using PlaidAccount = Going.Plaid.Entity.Account;

namespace Cardui.Api.Services.Plaid;

public interface IPlaidAccountsClient
{
    /// <summary>
    /// Fetches the accounts Plaid currently returns for this access token.
    /// </summary>
    Task<IReadOnlyList<PlaidAccount>> GetAccountsAsync(
        string accessToken,
        CancellationToken cancellationToken = default);
}
