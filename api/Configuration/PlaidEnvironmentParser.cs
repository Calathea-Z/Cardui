using Environment = Going.Plaid.Environment;

namespace Cardui.Api.Configuration;

public static class PlaidEnvironmentParser
{
    /// <summary>
    /// Parses a Plaid environment name. Throws when the name is not a known environment.
    /// </summary>
    public static Environment Parse(string environment)
    {
        if (!TryParse(environment, out var parsed))
        {
            throw new InvalidOperationException(
                $"Plaid environment '{environment}' is invalid. Expected one of: {string.Join(", ", GetValidNames())}.");
        }

        return parsed;
    }

    /// <summary>
    /// Tries to parse a Plaid environment name, ignoring case. Blank input fails.
    /// </summary>
    public static bool TryParse(string environment, out Environment parsed)
    {
        if (string.IsNullOrWhiteSpace(environment))
        {
            parsed = default;
            return false;
        }

        return Enum.TryParse(environment, true, out parsed);
    }

    /// <summary>
    /// Returns the Plaid environment names accepted by configuration.
    /// </summary>
    public static IReadOnlyList<string> GetValidNames() =>
        Enum.GetNames<Environment>();
}
