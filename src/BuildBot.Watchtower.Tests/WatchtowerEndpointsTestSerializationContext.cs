using System.Text.Json.Serialization;
using BuildBot.ServiceModel.Watchtower;

namespace BuildBot.Watchtower.Tests;

[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Default,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DictionaryKeyPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = false,
    IncludeFields = false
)]
[JsonSerializable(typeof(WatchTowerMessage))]
internal sealed partial class WatchtowerEndpointsTestSerializationContext : JsonSerializerContext;
