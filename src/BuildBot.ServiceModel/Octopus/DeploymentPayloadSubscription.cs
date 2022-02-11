using System.Text.Json.Serialization;

namespace BuildBot.ServiceModel.Octopus;

public sealed class DeploymentPayloadSubscription
{
    [JsonPropertyName("Id")]
    public string Id { get; set; } = default!;

    [JsonPropertyName("Name")]
    public string Name { get; set; } = default!;

    [JsonPropertyName("Type")]
    public int Type { get; set; }

    [JsonPropertyName("IsDisabled")]
    public bool IsDisabled { get; set; }
}