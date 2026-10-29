using System.Collections.Generic;
using System.Text.Json.Serialization;
using BuildBot.Json.Models;
using BuildBot.ServiceModel.ComponentStatus;

namespace BuildBot.Health.Tests;

[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Default,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DictionaryKeyPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = false,
    IncludeFields = false
)]
[JsonSerializable(typeof(PongDto))]
[JsonSerializable(typeof(IReadOnlyList<ServiceStatus>))]
internal sealed partial class StatusEndpointsTestSerializationContext : JsonSerializerContext;
