using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace BuildBot.ServiceModel.GitHub
{
    public class Push
    {
        [JsonPropertyName(name: "ref")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public string Ref { get; set; } = default!;

        [JsonPropertyName(name: "before")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public string Before { get; set; } = default!;

        [JsonPropertyName(name: "after")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public string After { get; set; } = default!;

        [JsonPropertyName(name: "head_commit")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public Commit HeadCommit { get; set; } = default!;

        [JsonPropertyName(name: "commits")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public List<Commit> Commits { get; set; } = default!;

        [JsonPropertyName(name: "repository")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public Repository Repository { get; set; } = default!;

        [JsonPropertyName(name: "pusher")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public Pusher Pusher { get; set; } = default!;

        [SuppressMessage(category: "Microsoft.Design", checkId: "CA1056:UriPropertiesShouldNotBeStrings", Justification = "Model for serialization")]
        [JsonPropertyName(name: "compare")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public string CompareUrl { get; set; } = default!;
    }
}