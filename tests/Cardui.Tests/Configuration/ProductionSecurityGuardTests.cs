using Cardui.Api.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Cardui.Tests.Configuration;

public class ProductionSecurityGuardTests
{
    [Fact]
    public void Ensure_RejectsLocalhostInProduction()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ProductionSecurityGuard.Ensure(
                new TestEnvironment("Production"),
                Configuration(new Dictionary<string, string?>
                {
                    ["AllowedHosts"] = "localhost",
                    ["Cors:AllowedOrigins:0"] = "http://localhost:3000",
                    ["Clerk:AuthorizedParties:0"] = "http://localhost:3000"
                })));

        Assert.Contains("AllowedHosts", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Ensure_AllowsAPublicHttpsOrigin()
    {
        ProductionSecurityGuard.Ensure(
            new TestEnvironment("Production"),
            Configuration(new Dictionary<string, string?>
            {
                ["AllowedHosts"] = "api.example.com",
                ["Cors:AllowedOrigins:0"] = "https://app.example.com",
                ["Clerk:AuthorizedParties:0"] = "https://app.example.com"
            }));
    }

    [Fact]
    public void Ensure_LeavesDevelopmentUnchanged()
    {
        ProductionSecurityGuard.Ensure(
            new TestEnvironment("Development"),
            Configuration(new Dictionary<string, string?>
            {
                ["AllowedHosts"] = "*"
            }));
    }

    private static IConfiguration Configuration(Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    private sealed class TestEnvironment : IHostEnvironment
    {
        public TestEnvironment(string environmentName)
        {
            EnvironmentName = environmentName;
        }

        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; } = "Cardui";
        public string ContentRootPath { get; set; } = ".";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
