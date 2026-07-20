using Cardui.Api.Exceptions;
using Going.Plaid;
using Going.Plaid.Entity;
using Microsoft.Extensions.Options;
using PlaidConfig = Cardui.Api.Options.PlaidOptions;

namespace Cardui.Api.Services.Plaid;

public class PlaidRequestExecutor : IPlaidRequestExecutor
{
    private readonly PlaidConfig _plaidOptions;
    private readonly ILogger<PlaidRequestExecutor> _logger;

    public PlaidRequestExecutor(
        IOptions<PlaidConfig> plaidOptions,
        ILogger<PlaidRequestExecutor> logger)
    {
        _plaidOptions = plaidOptions.Value;
        _logger = logger;
    }

    public TRequest WithCredentials<TRequest>(
        TRequest request,
        string? accessToken = null)
        where TRequest : RequestBase
    {
        request.ClientId = _plaidOptions.ClientId;
        request.Secret = _plaidOptions.Secret;

        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            request.AccessToken = accessToken;
        }

        return request;
    }

    public async Task<TResponse> ExecuteAsync<TResponse>(Func<Task<TResponse>> action)
        where TResponse : ResponseBase
    {
        try
        {
            var response = await action();

            return response.Error is not null
                ? throw CreatePlaidSyncException(response.Error, response.RequestId)
                : response;
        }
        catch (PlaidSyncException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Plaid API request failed");
            throw new PlaidSyncException(
                "Unable to reach Plaid. Please try again.",
                innerException: ex);
        }
    }

    private PlaidSyncException CreatePlaidSyncException(
        PlaidError error,
        string? requestId)
    {
        _logger.LogError(
            "Plaid API error {ErrorType}/{ErrorCode} (RequestId: {RequestId})",
            error.ErrorType,
            error.ErrorCode,
            error.RequestId ?? requestId);

        return new PlaidSyncException(
            GetUserSafePlaidMessage(error),
            error.ErrorCode,
            error.ErrorType);
    }

    private static string GetUserSafePlaidMessage(PlaidError ex)
    {
        if (!string.IsNullOrWhiteSpace(ex.DisplayMessage))
        {
            return ex.DisplayMessage;
        }

        return !string.IsNullOrWhiteSpace(ex.ErrorMessage)
            ? ex.ErrorMessage
            : "Plaid request failed. Please try again.";
    }
}
