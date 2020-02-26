using System.Text.Json.Serialization;

namespace BuildBot.ServiceModel.GitHub
{
    public sealed class CommitUser
    {
        [JsonPropertyName("name")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public string Name { get; set; } = default!;

        [JsonPropertyName("email")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public string Email { get; set; } = default!;

        [JsonPropertyName("username")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public string Username { get; set; } = default!;
    }
}