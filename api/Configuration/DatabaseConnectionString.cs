using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Cardui.Api.Configuration;

public static class DatabaseConnectionString
{
    /// <summary>
    /// Returns ConnectionStrings:DefaultConnection, or converts DATABASE_URL
    /// into an Npgsql connection string.
    /// </summary>
    public static string Get(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        var databaseUrl = configuration["DATABASE_URL"];

        if (!string.IsNullOrWhiteSpace(databaseUrl))
        {
            return ConvertDatabaseUrl(databaseUrl);
        }

        throw new InvalidOperationException(
            "Database connection is not configured. Set ConnectionStrings:DefaultConnection or DATABASE_URL.");
    }

    #region Private Methods

    /// <summary>
    /// Converts a postgres:// URL into an Npgsql connection string.
    /// sslmode is kept. Any other query parameter is rejected so it is not dropped.
    /// </summary>
    private static string ConvertDatabaseUrl(string databaseUrl)
    {
        var uri = new Uri(databaseUrl);
        var userInfo = uri.UserInfo.Split(':', 2);

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Database = uri.AbsolutePath.TrimStart('/'),
            Username = Uri.UnescapeDataString(userInfo[0]),
            Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty
        };

        ApplyQuery(builder, uri.Query);
        return builder.ConnectionString;
    }

    /// <summary>
    /// Copies sslmode and other known query parameters onto the Npgsql connection.
    /// </summary>
    private static void ApplyQuery(NpgsqlConnectionStringBuilder builder, string query)
    {
        var trimmed = query.TrimStart('?');
        if (trimmed.Length == 0)
        {
            return;
        }

        foreach (var pair in trimmed.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            var key = Uri.UnescapeDataString(parts[0]);
            var value = parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : string.Empty;
            if (key.Equals("sslmode", StringComparison.OrdinalIgnoreCase))
            {
                builder.SslMode = ParseSslMode(value);
                continue;
            }

            throw new InvalidOperationException(
                $"DATABASE_URL parameter '{key}' is not supported.");
        }
    }

    /// <summary>
    /// Maps a postgres sslmode value onto the Npgsql SSL mode.
    /// </summary>
    private static SslMode ParseSslMode(string value)
    {
        return value.ToLowerInvariant() switch
        {
            "disable" => SslMode.Disable,
            "allow" => SslMode.Allow,
            "prefer" => SslMode.Prefer,
            "require" => SslMode.Require,
            "verify-ca" => SslMode.VerifyCA,
            "verify-full" => SslMode.VerifyFull,
            _ => throw new InvalidOperationException(
                $"DATABASE_URL sslmode '{value}' is not supported.")
        };
    }

    #endregion
}
