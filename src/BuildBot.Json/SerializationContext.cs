using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using BuildBot.ServiceModel.GitHub;
using BuildBot.ServiceModel.Octopus;

namespace BuildBot.Json;

[SuppressMessage(category: "ReSharper", checkId: "PartialTypeWithSinglePart", Justification = "Required for JsonSerializerContext")]
[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Serialization | JsonSourceGenerationMode.Metadata,
                             PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
                             DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                             WriteIndented = false,
                             IncludeFields = false)]
[JsonSerializable(typeof(Author))]
[JsonSerializable(typeof(Branch))]
[JsonSerializable(typeof(Commit))]
[JsonSerializable(typeof(CommitUser))]
[JsonSerializable(typeof(Owner))]
[JsonSerializable(typeof(PingModel))]
[JsonSerializable(typeof(Push))]
[JsonSerializable(typeof(Pusher))]
[JsonSerializable(typeof(Repository))]
[JsonSerializable(typeof(Status))]
[JsonSerializable(typeof(StatusCommit))]
[JsonSerializable(typeof(Deploy))]
[JsonSerializable(typeof(DeploymentEvent))]
[JsonSerializable(typeof(DeploymentPayloadSubscription))]
[JsonSerializable(typeof(DeployMessageReference))]
[JsonSerializable(typeof(DeployPayload))]
internal sealed partial class SerializationContext : JsonSerializerContext
{
    // Code generated
}