using System.Diagnostics;
using System.Text.Json.Serialization;

namespace BuildBot.ServiceModel.GitHub;

[DebuggerDisplay("{Name}: {Email}")]
public readonly record struct Pusher
{
    [JsonConstructor]
    public Pusher(string name, string email)
    {
        this.Name = name;
        this.Email = email;
    }

    [JsonPropertyName(name: "name")]

    public string Name { get; }

    [JsonPropertyName(name: "email")]

    public string Email { get; }
}