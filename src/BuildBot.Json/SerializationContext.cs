using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using BuildBot.ServiceModel.Octopus;

namespace BuildBot.Json;

[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Serialization | JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(Deploy))]
[JsonSerializable(typeof(DeployPayload))]
[JsonSerializable(typeof(DeploymentPayloadSubscription))]
[JsonSerializable(typeof(DeploymentEvent))]
[JsonSerializable(typeof(DeployMessageReference))]
[SuppressMessage(category: "ReSharper", checkId: "PartialTypeWithSinglePart", Justification = "Required for " + nameof(JsonSerializerContext) + " code generation")]
internal sealed partial class SerializationContext : JsonSerializerContext
{
}