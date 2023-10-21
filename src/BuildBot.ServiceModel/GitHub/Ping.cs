using System.Diagnostics;
using System.Text.Json.Serialization;

namespace BuildBot.ServiceModel.GitHub;

[DebuggerDisplay("{Zen}: {HookId}")]
public readonly record struct Ping
{
    [JsonConstructor]
    public Ping(string zen, string hookId)
    {
        this.Zen = zen;
        this.HookId = hookId;
    }

    [JsonPropertyName(name: "zen")]
    public string Zen { get; }

    [JsonPropertyName(name: "hook_id")]
    public string HookId { get; }
}