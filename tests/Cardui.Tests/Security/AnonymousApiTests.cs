using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Cardui.Tests.Security;

public class AnonymousApiTests : IClassFixture<CarduiApiFactory>
{
    private readonly CarduiApiFactory _factory;

    public AnonymousApiTests(CarduiApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Health_IsPublic()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Accounts_RequireASignedInUser()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/api/accounts");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

public sealed class CarduiApiFactory : WebApplicationFactory<Program>
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
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] =
                    "Host=127.0.0.1;Port=1;Database=cardui;Username=cardui_user;Password=test",
                ["Plaid:ClientId"] = "client",
                ["Plaid:Secret"] = "secret",
                ["Plaid:Environment"] = "sandbox",
                ["Plaid:DefaultClientUserId"] = "dev-user",
                ["Clerk:Issuer"] = "https://example.clerk.accounts.dev",
                ["Clerk:AuthorizedParties:0"] = "https://app.example.com",
                ["DataProtection:KeysPath"] = Path.Combine(Path.GetTempPath(), "cardui-test-keys")
            });
        });
    }
}
