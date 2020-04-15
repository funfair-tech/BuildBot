using System.Text.Json.Serialization;

namespace BuildBot.ServiceModel.GitHub
{
    public sealed class Ping
    {
        [JsonPropertyName(name: "zen")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public string Zen { get; set; } = default!;

        [JsonPropertyName(name: "hook_id")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public string HookId { get; set; } = default!;
    }
}