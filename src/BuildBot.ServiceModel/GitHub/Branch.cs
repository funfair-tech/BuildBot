using System.Diagnostics;
using System.Text.Json.Serialization;

namespace BuildBot.ServiceModel.GitHub;

[DebuggerDisplay("{Name}")]
public readonly record struct Branch
{
    [JsonConstructor]
    public Branch(string name)
    {
        this.Name = name;
    }

    [JsonPropertyName(name: "name")]
    public string Name { get; }
}
