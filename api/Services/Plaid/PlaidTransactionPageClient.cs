using Cardui.Api.Dtos.Plaid;
using Going.Plaid.Entity;
using Going.Plaid.Transactions;

namespace Cardui.Api.Services.Plaid;

public class PlaidTransactionPageClient : IPlaidTransactionPageClient
{
    private const int PageSize = 100;

    private readonly IPlaidClientSource _clientSource;
    private readonly IPlaidRequestExecutor _requestExecutor;

    public PlaidTransactionPageClient(
        IPlaidClientSource clientSource,
        IPlaidRequestExecutor requestExecutor)
    {
        _clientSource = clientSource;
        _requestExecutor = requestExecutor;
    }

    /// <inheritdoc />
    public async Task<PlaidTransactionPageDto> GetPageAsync(
        string accessToken,
        string? cursor,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var client = _clientSource.GetClient();

        var request = _requestExecutor.WithCredentials(new TransactionsSyncRequest
        {
            Cursor = cursor,
            Count = PageSize,
            Options = new TransactionsSyncRequestOptions
            {
                IncludeOriginalDescription = true
            }
        }, accessToken);

        var response = await _requestExecutor.ExecuteAsync(
            () => client.TransactionsSyncAsync(request));

        return new PlaidTransactionPageDto
        {
            Added = response.Added ?? [],
            Modified = response.Modified ?? [],
            Removed = response.Removed ?? [],
            NextCursor = response.NextCursor,
            HasMore = response.HasMore
        };
    }
}
