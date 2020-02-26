using System.Text.Json.Serialization;

namespace BuildBot.ServiceModel.GitHub
{
    public sealed class Ping
    {
        [JsonPropertyName("zen")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public string Zen { get; set; } = default!;

        [JsonPropertyName("hook_id")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public string HookId { get; set; } = default!;
    }
}