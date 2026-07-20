using Cardui.Api.Options;
using Going.Plaid;
using Microsoft.Extensions.Options;
using PlaidConfig = Cardui.Api.Options.PlaidOptions;

namespace Cardui.Api.Configuration;

public static class PlaidServiceCollectionExtensions
{
    public static IServiceCollection AddCarduiPlaid(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<PlaidConfig>()
            .Bind(configuration.GetSection("Plaid"))
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<PlaidConfig>, PlaidOptionsValidator>();
        services.AddPlaidHttpClient();
        services.AddSingleton<PlaidClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<PlaidConfig>>().Value;
            var environment = PlaidEnvironmentParser.Parse(options.Environment);

            return new PlaidClient(
                environment,
                clientId: options.ClientId,
                secret: options.Secret,
                httpClientFactory: sp.GetRequiredService<IHttpClientFactory>(),
                logger: sp.GetRequiredService<ILogger<PlaidClient>>());
        });

        return services;
    }
}
