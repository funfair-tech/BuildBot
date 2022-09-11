using System.Text.Json.Serialization;

namespace BuildBot.ServiceModel.GitHub;

public sealed class Branch
{
    [JsonConstructor]
    public Branch(string name)
    {
        this.Name = name;
    }

    [JsonPropertyName(name: "name")]
    public string Name { get; }
}