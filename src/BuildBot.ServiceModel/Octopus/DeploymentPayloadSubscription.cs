using System.Diagnostics;
using System.Text.Json.Serialization;

namespace BuildBot.ServiceModel.Octopus;

[DebuggerDisplay("{Id}: {Name}")]
public readonly record struct DeploymentPayloadSubscription
{
    [JsonConstructor]
    public DeploymentPayloadSubscription(string id, string name, string type, bool isDisabled)
    {
        this.Id = id;
        this.Name = name;
        this.Type = type;
        this.IsDisabled = isDisabled;
    }

    [JsonPropertyName("Id")]
    public string Id { get; }

    [JsonPropertyName("Name")]
    public string Name { get; }

    [JsonPropertyName("Type")]
    public string Type { get; }

    [JsonPropertyName("IsDisabled")]
    public bool IsDisabled { get; }
}