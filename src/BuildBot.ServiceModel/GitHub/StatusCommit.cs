using System.Text.Json.Serialization;

namespace BuildBot.ServiceModel.GitHub
{
    public sealed class StatusCommit
    {
        [JsonPropertyName("commit")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public Commit Commit { get; set; } = default!;

        [JsonPropertyName("sha")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public string Sha { get; set; } = default!;

        [JsonPropertyName("author")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public Author Author { get; set; } = default!;
    }
}