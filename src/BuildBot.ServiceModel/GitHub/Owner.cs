using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace BuildBot.ServiceModel.GitHub;

[DebuggerDisplay("{AvatarUrl}")]
public readonly record struct Owner
{
    [JsonConstructor]
    public Owner(string avatarUrl)
    {
        this.AvatarUrl = avatarUrl;
    }

    [SuppressMessage(
        category: "Microsoft.Design",
        checkId: "CA1056:UriPropertiesShouldNotBeStrings",
        Justification = "Model for serialization"
    )]
    [JsonPropertyName(name: "avatar_url")]
    public string AvatarUrl { get; }
}
