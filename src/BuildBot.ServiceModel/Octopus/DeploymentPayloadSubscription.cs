using System.Text.Json.Serialization;

namespace BuildBot.ServiceModel.Octopus;

public sealed class DeploymentPayloadSubscription
{
    [JsonPropertyName("Id")]
    public string Id { get; init; } = default!;

    [JsonPropertyName("Name")]
    public string Name { get; init; } = default!;

    [JsonPropertyName("Type")]
    public string Type { get; init; } = default!;

    [JsonPropertyName("IsDisabled")]
    public bool IsDisabled { get; init; }
}