using Cardui.Api.Configuration;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Xunit;

namespace Cardui.Tests.Configuration;

public class DatabaseConnectionStringTests
{
    [Fact]
    public void Get_KeepsSslModeFromDatabaseUrl()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DATABASE_URL"] = "postgres://cardui_user:secret@db.example:5432/cardui?sslmode=require"
            })
            .Build();

        var builder = new NpgsqlConnectionStringBuilder(DatabaseConnectionString.Get(configuration));

        Assert.Equal(SslMode.Require, builder.SslMode);
        Assert.Equal("db.example", builder.Host);
        Assert.Equal("cardui", builder.Database);
        Assert.Equal("cardui_user", builder.Username);
    }

    [Fact]
    public void Get_RejectsAnUnknownDatabaseUrlParameter()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DATABASE_URL"] = "postgres://cardui_user:secret@db.example:5432/cardui?application_name=cardui"
            })
            .Build();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            DatabaseConnectionString.Get(configuration));

        Assert.Contains("application_name", exception.Message, StringComparison.Ordinal);
    }
}
