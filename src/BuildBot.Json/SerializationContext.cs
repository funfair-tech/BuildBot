using System.Text.Json.Serialization;
using BuildBot.ServiceModel.Octopus;

namespace BuildBot.Json;

[JsonSerializable(typeof(Deploy))]
[JsonSerializable(typeof(DeployPayload))]
[JsonSerializable(typeof(DeploymentPayloadSubscription))]
[JsonSerializable(typeof(DeploymentEvent))]
[JsonSerializable(typeof(DeployMessageReference))]
internal sealed partial class SerializationContext : JsonSerializerContext
{
}