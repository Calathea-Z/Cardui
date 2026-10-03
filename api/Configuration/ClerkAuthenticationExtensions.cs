using Cardui.Api.Options;
using Cardui.Api.Security;
using Microsoft.AspNetCore.Authorization;

namespace Cardui.Api.Configuration;

public static class ClerkAuthenticationExtensions
{
    public static IServiceCollection AddCarduiClerkAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<ClerkOptions>()
            .Bind(configuration.GetSection(ClerkOptions.SectionName));

        services.AddHttpContextAccessor();
        services.AddHttpClient(ClerkAuthenticationDefaults.Scheme);
        services.AddSingleton<IClerkSigningKeySource, ClerkSigningKeySource>();
        services.AddSingleton<ClerkSessionTokenValidator>();
        services.AddScoped<IHouseholdOwnerContext, HttpHouseholdOwnerContext>();

        services.AddAuthentication(ClerkAuthenticationDefaults.Scheme)
            .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, ClerkAuthenticationHandler>(
                ClerkAuthenticationDefaults.Scheme,
                _ => { });
        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });

        return services;
    }
}
