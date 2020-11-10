using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace BuildBot.ServiceModel.GitHub
{
    public sealed class Commit
    {
        [JsonPropertyName(name: "id")]
        [SuppressMessage(category: "ReSharper", checkId: "RedundantDefaultMemberInitializer", Justification = "TODO: Review")]
        public string Id { get; set; } = default!;

        [JsonPropertyName(name: "sha")]
        [SuppressMessage(category: "ReSharper", checkId: "RedundantDefaultMemberInitializer", Justification = "TODO: Review")]
        public string Sha { get; set; } = default!;

        [JsonPropertyName(name: "tree_id")]
        [SuppressMessage(category: "ReSharper", checkId: "RedundantDefaultMemberInitializer", Justification = "TODO: Review")]
        public string TreeId { get; set; } = default!;

        [JsonPropertyName(name: "distinct")]
        public bool Distinct { get; set; }

        [JsonPropertyName(name: "message")]
        [SuppressMessage(category: "ReSharper", checkId: "RedundantDefaultMemberInitializer", Justification = "TODO: Review")]
        public string Message { get; set; } = default!;

        [JsonPropertyName(name: "timestamp")]
        public DateTime TimeStamp { get; set; }

        [SuppressMessage(category: "Microsoft.Design", checkId: "CA1056:UriPropertiesShouldNotBeStrings", Justification = "Model for serialization")]
        [JsonPropertyName(name: "url")]
        [SuppressMessage(category: "ReSharper", checkId: "RedundantDefaultMemberInitializer", Justification = "TODO: Review")]
        public string Url { get; set; } = default!;

        [JsonPropertyName(name: "added")]
        [SuppressMessage(category: "ReSharper", checkId: "RedundantDefaultMemberInitializer", Justification = "TODO: Review")]
        public IReadOnlyList<string> Added { get; set; } = default!;

        [JsonPropertyName(name: "removed")]
        [SuppressMessage(category: "ReSharper", checkId: "RedundantDefaultMemberInitializer", Justification = "TODO: Review")]
        public IReadOnlyList<string> Removed { get; set; } = default!;

        [JsonPropertyName(name: "modified")]
        [SuppressMessage(category: "ReSharper", checkId: "RedundantDefaultMemberInitializer", Justification = "TODO: Review")]
        public IReadOnlyList<string> Modified { get; set; } = default!;

        [JsonPropertyName(name: "author")]
        [SuppressMessage(category: "ReSharper", checkId: "RedundantDefaultMemberInitializer", Justification = "TODO: Review")]
        public CommitUser Author { get; set; } = default!;

        [JsonPropertyName(name: "committer")]
        [SuppressMessage(category: "ReSharper", checkId: "RedundantDefaultMemberInitializer", Justification = "TODO: Review")]
        public CommitUser Committer { get; set; } = default!;
    }
}