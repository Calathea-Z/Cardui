using Cardui.Api.Configuration;
using Microsoft.Extensions.Options;

namespace Cardui.Api.Options;

public sealed class PlanningModelOptionsValidator : IValidateOptions<PlanningModelOptions>
{
    /// <summary>
    /// Checks planning-model settings before the API starts.
    /// Blank settings are allowed so the API can start with guided conversation off.
    /// A partial set is rejected. The model id is not chosen here.
    /// </summary>
    public ValidateOptionsResult Validate(string? name, PlanningModelOptions options)
    {
        if (!PlanningModelConfiguration.HasAnySetting(options))
        {
            return ValidateOptionsResult.Success;
        }

        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            failures.Add("PlanningModel:ApiKey is required.");
        }

        if (string.IsNullOrWhiteSpace(options.Model))
        {
            failures.Add("PlanningModel:Model is required.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
