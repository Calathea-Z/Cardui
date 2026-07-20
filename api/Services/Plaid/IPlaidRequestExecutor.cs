using Going.Plaid;

namespace Cardui.Api.Services.Plaid;

public interface IPlaidRequestExecutor
{
    TRequest WithCredentials<TRequest>(TRequest request, string? accessToken = null)
        where TRequest : RequestBase;

    Task<TResponse> ExecuteAsync<TResponse>(Func<Task<TResponse>> action)
        where TResponse : ResponseBase;
}
