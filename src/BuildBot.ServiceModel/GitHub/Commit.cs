using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace BuildBot.ServiceModel.GitHub
{
    public sealed class Commit
    {
        [JsonPropertyName(name: "id")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public string Id { get; set; } = default!;

        [JsonPropertyName(name: "sha")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public string Sha { get; set; } = default!;

        [JsonPropertyName(name: "tree_id")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public string TreeId { get; set; } = default!;

        [JsonPropertyName(name: "distinct")]
        public bool Distinct { get; set; }

        [JsonPropertyName(name: "message")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public string Message { get; set; } = default!;

        [JsonPropertyName(name: "timestamp")]
        public DateTime TimeStamp { get; set; }

        [SuppressMessage(category: "Microsoft.Design", checkId: "CA1056:UriPropertiesShouldNotBeStrings", Justification = "Model for serialization")]
        [JsonPropertyName(name: "url")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public string Url { get; set; } = default!;

        [JsonPropertyName(name: "added")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public List<string> Added { get; set; } = default!;

        [JsonPropertyName(name: "removed")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public List<string> Removed { get; set; } = default!;

        [JsonPropertyName(name: "modified")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public List<string> Modified { get; set; } = default!;

        [JsonPropertyName(name: "author")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public CommitUser Author { get; set; } = default!;

        [JsonPropertyName(name: "committer")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public CommitUser Committer { get; set; } = default!;
    }
}