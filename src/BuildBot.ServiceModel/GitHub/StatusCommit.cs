using System.Text.Json.Serialization;

namespace BuildBot.ServiceModel.GitHub
{
    public sealed class StatusCommit
    {
        [JsonPropertyName(name: "commit")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public Commit Commit { get; set; } = default!;

        [JsonPropertyName(name: "sha")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public string Sha { get; set; } = default!;

        [JsonPropertyName(name: "author")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public Author Author { get; set; } = default!;
    }
}