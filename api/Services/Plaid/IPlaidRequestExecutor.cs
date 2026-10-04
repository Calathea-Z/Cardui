using Going.Plaid;

namespace Cardui.Api.Services.Plaid;

public interface IPlaidRequestExecutor
{
    /// <summary>
    /// Copies the configured client id and secret onto a Plaid request.
    /// An access token is set only when one is supplied.
    /// </summary>
    TRequest WithCredentials<TRequest>(TRequest request, string? accessToken = null)
        where TRequest : RequestBase;

    /// <summary>
    /// Runs a Plaid call and turns a Plaid error or a transport failure
    /// into a PlaidSyncException. A PlaidSyncException is rethrown as-is.
    /// </summary>
    Task<TResponse> ExecuteAsync<TResponse>(Func<Task<TResponse>> action)
        where TResponse : ResponseBase;
}
