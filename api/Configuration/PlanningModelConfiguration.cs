using Cardui.Api.Options;

namespace Cardui.Api.Configuration;

public static class PlanningModelConfiguration
{
    /// <summary>
    /// OpenAI Responses endpoint. The model id is configuration, not this address.
    /// </summary>
    public static readonly Uri Endpoint = new("https://api.openai.com/v1/");

    /// <summary>
    /// Stops a hung call. Retry and cost limits stay in Phase 4 item 9.
    /// </summary>
    public static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// True when the API key and model id are both set.
    /// </summary>
    public static bool IsConfigured(PlanningModelOptions options)
    {
        return HasText(options.ApiKey) && HasText(options.Model);
    }

    /// <summary>
    /// True when either planning-model setting is present.
    /// </summary>
    public static bool HasAnySetting(PlanningModelOptions options)
    {
        return HasText(options.ApiKey) || HasText(options.Model);
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
