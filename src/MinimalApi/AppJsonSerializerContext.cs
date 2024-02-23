using System.Text.Json.Serialization;
using BuildBot.ServiceModel.GitHub;
using BuildBot.ServiceModel.Octopus;
using MinimalApi.Models;

namespace MinimalApi;

[JsonSerializable(typeof(PongDto))]
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
[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Serialization | JsonSourceGenerationMode.Metadata,
                             PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
                             DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                             WriteIndented = false,
                             IncludeFields = false)]
internal sealed partial class AppJsonSerializerContext : JsonSerializerContext
{
    // partial class
}