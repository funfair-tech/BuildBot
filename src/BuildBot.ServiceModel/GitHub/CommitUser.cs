using System.Diagnostics;
using System.Text.Json.Serialization;

namespace BuildBot.ServiceModel.GitHub;

[DebuggerDisplay("{Username}: {Name}: {Email}")]
public sealed class CommitUser
{
    [JsonConstructor]
    public CommitUser(string name, string email, string? username)
    {
        this.Name = name;
        this.Email = email;
        this.Username = username;
    }

    [JsonPropertyName(name: "name")]
    public string Name { get; }

    [JsonPropertyName(name: "email")]
    public string Email { get; }

    [JsonPropertyName(name: "username")]
    public string? Username { get; }
}