using System.Text.Json.Serialization;

namespace BuildBot.ServiceModel.Octopus;

public sealed class DeploymentPayloadSubscription
{
    [JsonPropertyName("Id")]
    public string Id { get; set; } = default!;

    [JsonPropertyName("Name")]
    public string Name { get; set; } = default!;

    [JsonPropertyName("Type")]
    public string Type { get; set; } = default!;

    [JsonPropertyName("IsDisabled")]
    public bool IsDisabled { get; set; }
}