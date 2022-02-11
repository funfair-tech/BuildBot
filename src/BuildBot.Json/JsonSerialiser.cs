using System.Text.Json;
using System.Text.Json.Serialization;

namespace BuildBot.Json;

public static class JsonSerialiser
{
    public static JsonSerializerOptions Configure(JsonSerializerOptions jsonSerializerOptions)
    {
        jsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        jsonSerializerOptions.PropertyNameCaseInsensitive = false;
        jsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        jsonSerializerOptions.WriteIndented = false;
        jsonSerializerOptions.AddContext<SerializationContext>();

        return jsonSerializerOptions;
    }
}