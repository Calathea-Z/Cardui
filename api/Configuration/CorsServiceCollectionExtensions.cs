namespace Cardui.Api.Configuration;

public static class CorsServiceCollectionExtensions
{
    private static readonly string[] DefaultFrontendOrigins =
    [
        "http://localhost:3000",
        "https://localhost:3000"
    ];

    /// <summary>
    /// Allows the configured frontend origins, or localhost:3000 when none are configured.
    /// </summary>
    public static IServiceCollection AddCarduiCors(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var allowedOrigins = configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>();

        services.AddCors(options =>
        {
            options.AddPolicy(CorsPolicyNames.Frontend, policy =>
            {
                policy
                    .WithOrigins(allowedOrigins is { Length: > 0 }
                        ? allowedOrigins
                        : DefaultFrontendOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        return services;
    }
}
