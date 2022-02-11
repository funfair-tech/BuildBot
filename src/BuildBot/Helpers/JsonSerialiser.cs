using System.Text.Json;
using System.Text.Json.Serialization;

namespace BuildBot.Helpers;

public static class JsonSerialiser
{
    public static JsonSerializerOptions Configure(JsonSerializerOptions jsonSerializerOptions)
    {
        jsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        jsonSerializerOptions.PropertyNameCaseInsensitive = true;
        jsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        jsonSerializerOptions.WriteIndented = false;

        return jsonSerializerOptions;
    }
}