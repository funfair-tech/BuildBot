using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace BuildBot.ServiceModel.GitHub
{
    public class Push
    {
        [JsonPropertyName("ref")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public string Ref { get; set; } = default!;

        [JsonPropertyName("before")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public string Before { get; set; } = default!;

        [JsonPropertyName("after")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public string After { get; set; } = default!;

        [JsonPropertyName("head_commit")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public Commit HeadCommit { get; set; } = default!;

        [JsonPropertyName("commits")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public List<Commit> Commits { get; set; } = default!;

        [JsonPropertyName("repository")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public Repository Repository { get; set; } = default!;

        [JsonPropertyName("pusher")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public Pusher Pusher { get; set; } = default!;

        [SuppressMessage(category: "Microsoft.Design", checkId: "CA1056:UriPropertiesShouldNotBeStrings", Justification = "Model for serialization")]
        [JsonPropertyName("compare")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public string CompareUrl { get; set; } = default!;
    }
}