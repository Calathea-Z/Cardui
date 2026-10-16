using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cardui.Api.Domain.Plans;

/// <summary>
/// Reads and writes a decision plan as stable JSON.
/// The same plan always produces the same text, which is how a repeated save is recognized.
/// Enums are stored as their member names.
/// </summary>
public static class DecisionPlanJson
{
    private static readonly JsonSerializerOptions Options = CreateOptions();

    /// <summary>
    /// Writes the plan as one JSON document.
    /// </summary>
    public static string Serialize(DecisionPlan plan)
    {
        return JsonSerializer.Serialize(plan, Options);
    }

    /// <summary>
    /// Reads a plan that this class wrote.
    /// Throws when the text is empty or not a plan.
    /// </summary>
    public static DecisionPlan Deserialize(string json)
    {
        var plan = JsonSerializer.Deserialize<DecisionPlan>(json, Options);
        if (plan is null)
        {
            throw new JsonException("A saved plan could not be read.");
        }

        return plan;
    }

    #region Private Methods

    /// <summary>
    /// Uses camelCase names and rejects numeric enums so a stored plan stays readable.
    /// </summary>
    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never
        };
        options.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
        return options;
    }

    #endregion
}
