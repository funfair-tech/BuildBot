using System.Text.Json.Serialization;
using BuildBot.ServiceModel.CloudFormation;

namespace BuildBot.CloudFormation;

[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Serialization | JsonSourceGenerationMode.Metadata,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = false,
    IncludeFields = false
)]
[JsonSerializable(typeof(SnsMessage))]
internal sealed partial class CloudFormationSerializationContext : JsonSerializerContext;
