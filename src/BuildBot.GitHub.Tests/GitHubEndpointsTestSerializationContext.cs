using System.Text.Json;
using System.Text.Json.Serialization;
using BuildBot.ServiceModel.GitHub;

namespace BuildBot.GitHub.Tests;

[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Default,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DictionaryKeyPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = false,
    IncludeFields = false
)]
[JsonSerializable(typeof(PingModel))]
[JsonSerializable(typeof(Push))]
[JsonSerializable(typeof(Status))]
[JsonSerializable(typeof(JsonElement))]
internal sealed partial class GitHubEndpointsTestSerializationContext : JsonSerializerContext;
