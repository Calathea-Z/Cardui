using Cardui.Api.Configuration;
using Microsoft.Extensions.Options;

namespace Cardui.Api.Options;

public sealed class PlaidOptionsValidator : IValidateOptions<PlaidOptions>
{
    /// <summary>
    /// Checks required Plaid settings before the API starts.
    /// </summary>
    public ValidateOptionsResult Validate(string? name, PlaidOptions options)
    {
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

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
