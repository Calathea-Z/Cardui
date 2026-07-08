using Environment = Going.Plaid.Environment;

namespace Cardui.Api.Configuration;

public static class PlaidEnvironmentParser
{
    public static Environment Parse(string environment)
    {
        if (!TryParse(environment, out var parsed))
        {
            throw new InvalidOperationException(
                $"Plaid environment '{environment}' is invalid. Expected one of: {string.Join(", ", GetValidNames())}.");
        }

        return parsed;
    }

    public static bool TryParse(string environment, out Environment parsed)
    {
        if (string.IsNullOrWhiteSpace(environment))
        {
            parsed = default;
            return false;
        }

        return Enum.TryParse(environment, true, out parsed);
    }

    public static IReadOnlyList<string> GetValidNames() =>
        Enum.GetNames<Environment>();
}
