using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Cardui.Tests.Security;

public class OptionalPlanningModelStartupTests
{
    [Fact]
    public async Task Health_StartsWithoutAPlanningModel()
    {
        using var factory = new OptionalPlanningModelApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void Host_FailsWhenPlanningModelConfigurationIsPartial()
    {
        using var factory = new PartialPlanningModelApiFactory();

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

        Assert.Contains("PlanningModel:Model is required.", Flatten(exception));
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

public class OptionalPlanningModelApiFactory : PlanningModelStartupApiFactory
{
    /// <summary>
    /// Leaves planning-model settings blank.
    /// </summary>
    protected override void AddPlanningModel(Dictionary<string, string?> config)
    {
    }
}

public sealed class PartialPlanningModelApiFactory : PlanningModelStartupApiFactory
{
    /// <summary>
    /// Sets only the API key so startup must reject the partial configuration.
    /// </summary>
    protected override void AddPlanningModel(Dictionary<string, string?> config)
    {
        config["PlanningModel:ApiKey"] = "sk-test";
    }
}

public abstract class PlanningModelStartupApiFactory : WebApplicationFactory<Program>
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
            AddPlanningModel(settings);
            config.AddInMemoryCollection(settings);
        });
    }

    /// <summary>
    /// Adds the planning-model settings for this host, if any.
    /// </summary>
    protected abstract void AddPlanningModel(Dictionary<string, string?> config);
}
