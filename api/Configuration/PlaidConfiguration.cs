using Cardui.Api.Options;

namespace Cardui.Api.Configuration;

public static class PlaidConfiguration
{
    /// <summary>
    /// True when the client id, secret, and environment are all set.
    /// </summary>
    public static bool IsConfigured(PlaidOptions options)
    {
        return HasText(options.ClientId)
            && HasText(options.Secret)
            && HasText(options.Environment);
    }

    /// <summary>
    /// True when any Plaid setting other than a built-in default is present.
    /// </summary>
    public static bool HasAnySetting(PlaidOptions options)
    {
        return HasText(options.ClientId)
            || HasText(options.Secret)
            || HasText(options.Environment)
            || HasText(options.WebhookUrl);
    }

    #region Private Methods

    /// <summary>
    /// True when the value has non-whitespace text.
    /// </summary>
    private static bool HasText(string? value)
    {
        return !string.IsNullOrWhiteSpace(value);
    }

    #endregion
}
