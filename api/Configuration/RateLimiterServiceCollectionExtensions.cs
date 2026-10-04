using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Cardui.Api.Configuration;

public static class CarduiRateLimiting
{
    public const string PlaidPolicy = "plaid";

    /// <summary>
    /// Limits request bursts. Plaid routes get a tighter per-user window,
    /// and the webhook route is limited per caller address.
    /// </summary>
    public static IServiceCollection AddCarduiRateLimiter(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 300,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));

            options.AddPolicy(PlaidPolicy, context =>
            {
                if (context.Request.Path.StartsWithSegments("/api/plaid/webhook"))
                {
                    return RateLimitPartition.GetFixedWindowLimiter(
                        "webhook:" + (context.Connection.RemoteIpAddress?.ToString() ?? "unknown"),
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 120,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0
                        });
                }

                var userId = context.User.FindFirst("sub")?.Value
                    ?? context.Connection.RemoteIpAddress?.ToString()
                    ?? "anonymous";
                return RateLimitPartition.GetFixedWindowLimiter(
                    "user:" + userId,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 30,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    });
            });
        });

        return services;
    }
}
