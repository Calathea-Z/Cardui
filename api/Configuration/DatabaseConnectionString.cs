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

        return builder.ConnectionString;
    }

    #endregion
}
