using System.Text.Json.Serialization;

namespace BuildBot.ServiceModel.GitHub;

public sealed class Repository
{
    [JsonConstructor]
    public Repository(int id, string name, string fullName, Owner owner)
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