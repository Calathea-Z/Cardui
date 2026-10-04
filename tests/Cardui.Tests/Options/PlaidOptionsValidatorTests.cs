using Cardui.Api.Configuration;
using Cardui.Api.Options;
using Xunit;

namespace Cardui.Tests.Options;

public class PlaidOptionsValidatorTests
{
    [Fact]
    public void Validate_FailsWhenRequiredPlaidConfigurationIsMissing()
    {
        var validator = new PlaidOptionsValidator();

        var result = validator.Validate(
            null,
            new PlaidOptions
            {
                ClientId = "",
                Secret = "",
                Environment = "not-real",
                DefaultClientUserId = ""
            });

        Assert.True(result.Failed);
        Assert.Contains("Plaid:ClientId is required.", result.Failures);
        Assert.Contains("Plaid:Secret is required.", result.Failures);
        Assert.Contains("Plaid:DefaultClientUserId is required.", result.Failures);
        Assert.Contains(result.Failures, failure =>
            failure.StartsWith("Plaid:Environment 'not-real' is invalid."));
    }

    [Fact]
    public void Validate_SucceedsForCompletePlaidConfiguration()
    {
        var validator = new PlaidOptionsValidator();
        var environment = PlaidEnvironmentParser.GetValidNames()[0];

        var result = validator.Validate(
            null,
            new PlaidOptions
            {
                ClientId = "client-id",
                Secret = "secret",
                Environment = environment,
                DefaultClientUserId = "dev-user"
            });

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_SucceedsWhenPlaidCredentialsAreBlank()
    {
        var validator = new PlaidOptionsValidator();

        var result = validator.Validate(null, new PlaidOptions());

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_FailsWhenOnlySomePlaidCredentialsAreSet()
    {
        var validator = new PlaidOptionsValidator();

        var result = validator.Validate(
            null,
            new PlaidOptions
            {
                ClientId = "client-id"
            });

        Assert.True(result.Failed);
        Assert.Contains("Plaid:Secret is required.", result.Failures);
        Assert.Contains("Plaid:Environment is required.", result.Failures);
    }

    [Fact]
    public void Validate_FailsWhenWebhookIsSetWithoutCredentials()
    {
        var validator = new PlaidOptionsValidator();

        var result = validator.Validate(
            null,
            new PlaidOptions
            {
                WebhookUrl = "https://example.com/api/plaid/webhook"
            });

        Assert.True(result.Failed);
        Assert.Contains("Plaid:ClientId is required.", result.Failures);
        Assert.Contains("Plaid:Secret is required.", result.Failures);
        Assert.Contains("Plaid:Environment is required.", result.Failures);
    }
}
