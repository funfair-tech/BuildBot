using System.Diagnostics;
using System.Text.Json.Serialization;

namespace BuildBot.ServiceModel.GitHub;

[DebuggerDisplay("{FullName}")]
public readonly record struct Repository
{
    [JsonConstructor]
    public Repository(int id, string name, string fullName, in Owner owner)
    {
        this.Id = id;
        this.Name = name;
        this.FullName = fullName;
        this.Owner = owner;
    }

    [JsonPropertyName(name: "id")]
    public int Id { get; }

    [JsonPropertyName(name: "name")]

    public string Name { get; }

    [JsonPropertyName(name: "full_name")]

    public string FullName { get; }

    [JsonPropertyName(name: "owner")]

    public Owner Owner { get; }
}