using Going.Plaid.Link;

namespace Cardui.Api.Services.Plaid;

public class PlaidLinkClient : IPlaidLinkClient
{
    private readonly IPlaidClientSource _clientSource;
    private readonly IPlaidRequestExecutor _requestExecutor;

    public PlaidLinkClient(
        IPlaidClientSource clientSource,
        IPlaidRequestExecutor requestExecutor)
    {
        _clientSource = clientSource;
        _requestExecutor = requestExecutor;
    }

    /// <inheritdoc />
    public async Task<string?> CreateAsync(
        LinkTokenCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var client = _clientSource.GetClient();
        var response = await _requestExecutor.ExecuteAsync(
            () => client.LinkTokenCreateAsync(request));

        return response.LinkToken;
    }
}
