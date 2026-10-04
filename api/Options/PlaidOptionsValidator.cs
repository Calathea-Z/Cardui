using Cardui.Api.Configuration;
using Microsoft.Extensions.Options;

namespace Cardui.Api.Options;

public sealed class PlaidOptionsValidator : IValidateOptions<PlaidOptions>
{
    /// <summary>
    /// Checks Plaid settings before the API starts.
    /// Blank credentials are allowed so manual use can start.
    /// A partial or invalid set is rejected.
    /// </summary>
    public ValidateOptionsResult Validate(string? name, PlaidOptions options)
    {
        if (!PlaidConfiguration.HasAnySetting(options))
        {
            return ValidateOptionsResult.Success;
        }

        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.ClientId))
            failures.Add("Plaid:ClientId is required.");

        if (string.IsNullOrWhiteSpace(options.Secret))
            failures.Add("Plaid:Secret is required.");

        if (string.IsNullOrWhiteSpace(options.Environment))
        {
            failures.Add("Plaid:Environment is required.");
        }
        else if (!PlaidEnvironmentParser.TryParse(options.Environment, out _))
        {
            failures.Add(
                $"Plaid:Environment '{options.Environment}' is invalid. Expected one of: {string.Join(", ", PlaidEnvironmentParser.GetValidNames())}.");
        }

        if (string.IsNullOrWhiteSpace(options.DefaultClientUserId))
            failures.Add("Plaid:DefaultClientUserId is required.");

        if (!string.IsNullOrWhiteSpace(options.WebhookUrl)
            && !IsAbsoluteHttpUrl(options.WebhookUrl))
        {
            failures.Add("Plaid:WebhookUrl must be an absolute http or https URL.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    #region Private Methods

    /// <summary>
    /// True when the value is an absolute http or https URL.
    /// </summary>
    private static bool IsAbsoluteHttpUrl(string value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
    }

    #endregion
}
