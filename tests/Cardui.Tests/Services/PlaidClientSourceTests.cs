using Cardui.Api.Configuration;
using Cardui.Api.Exceptions;
using Cardui.Api.Services.Plaid;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Cardui.Tests.Services;

public class PlaidClientSourceTests
{
    [Fact]
    public void GetClient_ThrowsWhenCredentialsAreBlank()
    {
        using var provider = Build(new Dictionary<string, string?>());
        var source = provider.GetRequiredService<IPlaidClientSource>();

        var exception = Assert.Throws<PlaidNotConfiguredException>(() => source.GetClient());

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, exception.StatusCode);
        Assert.Equal("Bank linking is not configured.", exception.Message);
    }

    [Fact]
    public void GetClient_ReturnsClientWhenCredentialsAreComplete()
    {
        using var provider = Build(new Dictionary<string, string?>
        {
            ["Plaid:ClientId"] = "client",
            ["Plaid:Secret"] = "secret",
            ["Plaid:Environment"] = "sandbox",
            ["Plaid:DefaultClientUserId"] = "dev-user"
        });
        var source = provider.GetRequiredService<IPlaidClientSource>();

        Assert.NotNull(source.GetClient());
    }

    #region Private Methods

    /// <summary>
    /// Builds a provider with only the Plaid settings under test.
    /// </summary>
    private static ServiceProvider Build(Dictionary<string, string?> plaid)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(plaid)
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCarduiPlaid(configuration);
        return services.BuildServiceProvider();
    }

    #endregion
}
