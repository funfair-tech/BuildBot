using System.Text.Json;
using System.Text.Json.Serialization;

namespace BuildBot.Helpers;

internal static class JsonSerialiser
{
    public static JsonSerializerOptions Configure(JsonSerializerOptions jsonSerializerOptions)
    {
        jsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        jsonSerializerOptions.PropertyNameCaseInsensitive = false;
        jsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        jsonSerializerOptions.WriteIndented = false;

        return jsonSerializerOptions;
    }
}