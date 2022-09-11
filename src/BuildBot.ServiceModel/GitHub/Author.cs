using System.Diagnostics;
using System.Text.Json.Serialization;

namespace BuildBot.ServiceModel.GitHub;

[DebuggerDisplay("{Login}")]
public sealed class Author
{
    [JsonConstructor]
    public Author(string login)
    {
        this.Login = login;
    }

    [JsonPropertyName(name: "login")]
    public string Login { get; }
}