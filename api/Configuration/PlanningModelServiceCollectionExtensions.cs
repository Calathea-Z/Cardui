using Cardui.Api.Options;
using Cardui.Api.Services.Interfaces;
using Cardui.Api.Services.Planning;
using Microsoft.Extensions.Options;

namespace Cardui.Api.Configuration;

public static class PlanningModelServiceCollectionExtensions
{
    /// <summary>
    /// Registers the planning-model options and the OpenAI adapter.
    /// Blank settings are allowed. A partial configuration fails at startup.
    /// </summary>
    public static IServiceCollection AddCarduiPlanningModel(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<PlanningModelOptions>()
            .Bind(configuration.GetSection("PlanningModel"))
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<PlanningModelOptions>, PlanningModelOptionsValidator>();
        services.AddHttpClient<IPlanningModel, OpenAiPlanningModel>(client =>
        {
            client.BaseAddress = PlanningModelConfiguration.Endpoint;
        });

        return services;
    }
}
