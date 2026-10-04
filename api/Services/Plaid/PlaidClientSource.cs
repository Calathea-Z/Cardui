using Cardui.Api.Configuration;
using Cardui.Api.Exceptions;
using Going.Plaid;
using Microsoft.Extensions.Options;
using PlaidConfig = Cardui.Api.Options.PlaidOptions;

namespace Cardui.Api.Services.Plaid;

public sealed class PlaidClientSource : IPlaidClientSource
{
    private readonly PlaidClient? _client;

    public PlaidClientSource(
        IOptions<PlaidConfig> options,
        IHttpClientFactory httpClientFactory,
        ILogger<PlaidClient> logger)
    {
        var plaid = options.Value;
        if (!PlaidConfiguration.IsConfigured(plaid))
        {
            return;
        }

        _client = new PlaidClient(
            PlaidEnvironmentParser.Parse(plaid.Environment),
            clientId: plaid.ClientId,
            secret: plaid.Secret,
            httpClientFactory: httpClientFactory,
            logger: logger);
    }

    /// <inheritdoc />
    public PlaidClient GetClient()
    {
        return _client ?? throw new PlaidNotConfiguredException();
    }
}
