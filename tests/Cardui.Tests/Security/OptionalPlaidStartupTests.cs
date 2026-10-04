using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Cardui.Tests.Security;

public class OptionalPlaidStartupTests
{
    [Fact]
    public async Task Health_StartsWithoutPlaidCredentials()
    {
        using var factory = new OptionalPlaidApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PlaidItems_RequireASignedInUserWhenPlaidIsNotConfigured()
    {
        using var factory = new OptionalPlaidApiFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/api/plaid/items");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void Host_FailsWhenPlaidConfigurationIsPartial()
    {
        using var factory = new PartialPlaidApiFactory();

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

        Assert.Contains("Plaid:Secret is required.", Flatten(exception));
    }

    #region Private Methods

    /// <summary>
    /// Joins an exception and its inner exceptions so a wrapped startup failure can be asserted.
    /// </summary>
    private static string Flatten(Exception exception)
    {
        var messages = new List<string>();
        for (var current = exception; current is not null; current = current.InnerException)
        {
            messages.Add(current.Message);
        }

        return string.Join(" ", messages);
    }

    #endregion
}

public class OptionalPlaidApiFactory : PlaidStartupApiFactory
{
    /// <summary>
    /// Leaves Plaid credentials blank.
    /// </summary>
    protected override void AddPlaid(Dictionary<string, string?> config)
    {
    }
}

public sealed class PartialPlaidApiFactory : PlaidStartupApiFactory
{
    /// <summary>
    /// Sets only the client id so startup must reject the partial configuration.
    /// </summary>
    protected override void AddPlaid(Dictionary<string, string?> config)
    {
        config["Plaid:ClientId"] = "client";
    }
}

public abstract class PlaidStartupApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseDefaultServiceProvider((_, options) =>
        {
            options.ValidateScopes = true;
            options.ValidateOnBuild = true;
        });
        builder.ConfigureAppConfiguration((_, config) =>
        {
            var settings = new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] =
                    "Host=127.0.0.1;Port=1;Database=cardui;Username=cardui_user;Password=test",
                ["Clerk:Issuer"] = "https://example.clerk.accounts.dev",
                ["Clerk:AuthorizedParties:0"] = "https://app.example.com",
                ["DataProtection:KeysPath"] = Path.Combine(Path.GetTempPath(), "cardui-test-keys")
            };
            AddPlaid(settings);
            config.AddInMemoryCollection(settings);
        });
    }

    /// <summary>
    /// Adds the Plaid settings for this host, if any.
    /// </summary>
    protected abstract void AddPlaid(Dictionary<string, string?> config);
}
