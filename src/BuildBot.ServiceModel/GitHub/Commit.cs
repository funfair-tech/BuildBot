using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace BuildBot.ServiceModel.GitHub
{
    public sealed class Commit
    {
        [JsonPropertyName("id")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public string Id { get; set; } = default!;

        [JsonPropertyName("sha")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public string Sha { get; set; } = default!;

        [JsonPropertyName("tree_id")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public string TreeId { get; set; } = default!;

        [JsonPropertyName("distinct")]
        public bool Distinct { get; set; }

        [JsonPropertyName("message")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public string Message { get; set; } = default!;

        [JsonPropertyName("timestamp")]
        public DateTime TimeStamp { get; set; }

        [SuppressMessage(category: "Microsoft.Design", checkId: "CA1056:UriPropertiesShouldNotBeStrings", Justification = "Model for serialization")]
        [JsonPropertyName("url")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public string Url { get; set; } = default!;

        [JsonPropertyName("added")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public List<string> Added { get; set; } = default!;

        [JsonPropertyName("removed")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public List<string> Removed { get; set; } = default!;

        [JsonPropertyName("modified")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public List<string> Modified { get; set; } = default!;

        [JsonPropertyName("author")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public CommitUser Author { get; set; } = default!;

        [JsonPropertyName("committer")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public CommitUser Committer { get; set; } = default!;
    }
}