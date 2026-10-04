using Cardui.Api.Options;
using Cardui.Api.Services.Plaid;
using Going.Plaid;
using Microsoft.Extensions.Options;
using PlaidConfig = Cardui.Api.Options.PlaidOptions;

namespace Cardui.Api.Configuration;

public static class PlaidServiceCollectionExtensions
{
    /// <summary>
    /// Registers Plaid options and the client source.
    /// Blank credentials are allowed. A partial configuration fails at startup.
    /// </summary>
    public static IServiceCollection AddCarduiPlaid(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<PlaidConfig>()
            .Bind(configuration.GetSection("Plaid"))
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<PlaidConfig>, PlaidOptionsValidator>();
        services.AddPlaidHttpClient();
        services.AddSingleton<IPlaidClientSource, PlaidClientSource>();

        return services;
    }
}
