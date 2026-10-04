namespace Cardui.Api.Configuration;

public static class ProductionSecurityGuard
{
    /// <summary>
    /// Rejects a production host that still trusts localhost or a wildcard host header.
    /// Development keeps the local origins.
    /// </summary>
    public static void Ensure(IHostEnvironment environment, IConfiguration configuration)
    {
        if (!environment.IsProduction())
        {
            return;
        }

        var failures = new List<string>();
        RequirePublicHosts(configuration["AllowedHosts"], failures);
        RequirePublicOrigins(configuration.GetSection("Cors:AllowedOrigins").Get<string[]>(), "Cors:AllowedOrigins", failures);
        RequirePublicOrigins(
            configuration.GetSection("Clerk:AuthorizedParties").Get<string[]>(),
            "Clerk:AuthorizedParties",
            failures);
        RequireHttpsWebhook(configuration["Plaid:WebhookUrl"], failures);

        if (failures.Count > 0)
        {
            throw new InvalidOperationException(string.Join(" ", failures));
        }
    }

    #region Private Methods

    /// <summary>
    /// Requires at least one specific host, and rejects * and loopback names.
    /// </summary>
    private static void RequirePublicHosts(string? allowedHosts, List<string> failures)
    {
        var hosts = (allowedHosts ?? string.Empty)
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (hosts.Length == 0 || hosts.Any(host => host == "*" || IsLoopback(host)))
        {
            failures.Add(
                "AllowedHosts in production must name the public API host.");
        }
    }

    /// <summary>
    /// Requires at least one https origin, and rejects loopback origins.
    /// </summary>
    private static void RequirePublicOrigins(
        string[]? origins,
        string settingName,
        List<string> failures)
    {
        if (origins is not { Length: > 0 }
            || origins.Any(origin => !IsPublicHttpsOrigin(origin)))
        {
            failures.Add(
                $"{settingName} in production must be the public https origins.");
        }
    }

    /// <summary>
    /// Requires an https webhook URL when one is configured.
    /// </summary>
    private static void RequireHttpsWebhook(string? webhookUrl, List<string> failures)
    {
        if (string.IsNullOrWhiteSpace(webhookUrl))
        {
            return;
        }

        if (!Uri.TryCreate(webhookUrl, UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps)
        {
            failures.Add("Plaid:WebhookUrl in production must be an absolute https URL.");
        }
    }

    /// <summary>
    /// True when the origin is absolute https and the host is not loopback.
    /// </summary>
    private static bool IsPublicHttpsOrigin(string origin)
    {
        return Uri.TryCreate(origin, UriKind.Absolute, out var uri)
            && uri.Scheme == Uri.UriSchemeHttps
            && !IsLoopback(uri.Host);
    }

    /// <summary>
    /// True for localhost and loopback addresses.
    /// </summary>
    private static bool IsLoopback(string host)
    {
        return host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            || host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)
            || host.Equals("[::1]", StringComparison.OrdinalIgnoreCase)
            || host.Equals("::1", StringComparison.OrdinalIgnoreCase);
    }

    #endregion
}
