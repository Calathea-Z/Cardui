using Going.Plaid.Accounts;
using PlaidAccount = Going.Plaid.Entity.Account;

namespace Cardui.Api.Services.Plaid;

public class PlaidAccountsClient : IPlaidAccountsClient
{
    private readonly IPlaidClientSource _clientSource;
    private readonly IPlaidRequestExecutor _requestExecutor;

    public PlaidAccountsClient(
        IPlaidClientSource clientSource,
        IPlaidRequestExecutor requestExecutor)
    {
        _clientSource = clientSource;
        _requestExecutor = requestExecutor;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PlaidAccount>> GetAccountsAsync(
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var client = _clientSource.GetClient();
        var request = _requestExecutor.WithCredentials(
            new AccountsGetRequest(),
            accessToken);

        var response = await _requestExecutor.ExecuteAsync(
            () => client.AccountsGetAsync(request));

        return response.Accounts ?? [];
    }
}
