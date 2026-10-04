using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cardui.Api.Configuration;

public static class CarduiJsonSerializer
{
    /// <summary>
    /// Writes enums as their member names and rejects a numeric value.
    /// </summary>
    public static void UseEnumMemberNames(JsonSerializerOptions options)
    {
        options.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
    }
}
