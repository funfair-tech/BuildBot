using System.Text.Json.Serialization;

namespace BuildBot.ServiceModel.GitHub
{
    public sealed class CommitUser
    {
        [JsonPropertyName(name: "name")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public string Name { get; set; } = default!;

        [JsonPropertyName(name: "email")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public string Email { get; set; } = default!;

        [JsonPropertyName(name: "username")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public string Username { get; set; } = default!;
    }
}