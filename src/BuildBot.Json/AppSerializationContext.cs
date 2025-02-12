using System.Text.Json.Serialization;

namespace BuildBot.Json;

public static class AppSerializationContext
{
    public static JsonSerializerContext Default { get; } = SerializationContext.Default;
}
