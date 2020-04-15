using System.Text.Json.Serialization;

namespace BuildBot.ServiceModel.GitHub
{
    public sealed class Pusher
    {
        [JsonPropertyName(name: "name")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public string Name { get; set; } = default!;

        [JsonPropertyName(name: "email")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public string Email { get; set; } = default!;
    }
}