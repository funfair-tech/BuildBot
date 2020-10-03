using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace BuildBot.ServiceModel.GitHub
{
    public sealed class Status
    {
        [SuppressMessage(category: "Microsoft.Design", checkId: "CA1056:UriPropertiesShouldNotBeStrings", Justification = "Model for serialization")]
        [JsonPropertyName(name: "target_url")]
        [SuppressMessage(category: "ReSharper", checkId: "RedundantDefaultMemberInitializer", Justification = "TODO: Review")]
        public string TargetUrl { get; set; } = default!;

        [JsonPropertyName(name: "repository")]
        [SuppressMessage(category: "ReSharper", checkId: "RedundantDefaultMemberInitializer", Justification = "TODO: Review")]
        public Repository Repository { get; set; } = default!;

        [JsonPropertyName(name: "context")]
        [SuppressMessage(category: "ReSharper", checkId: "RedundantDefaultMemberInitializer", Justification = "TODO: Review")]
        public string Context { get; set; } = default!;

        [JsonPropertyName(name: "state")]
        [SuppressMessage(category: "ReSharper", checkId: "RedundantDefaultMemberInitializer", Justification = "TODO: Review")]
        public string State { get; set; } = default!;

        [JsonPropertyName(name: "branches")]
        [SuppressMessage(category: "ReSharper", checkId: "RedundantDefaultMemberInitializer", Justification = "TODO: Review")]
        public List<Branch> Branches { get; set; } = default!;

        [JsonPropertyName(name: "description")]
        [SuppressMessage(category: "ReSharper", checkId: "RedundantDefaultMemberInitializer", Justification = "TODO: Review")]
        public string Description { get; set; } = default!;

        [JsonPropertyName(name: "commit")]
        [SuppressMessage(category: "ReSharper", checkId: "RedundantDefaultMemberInitializer", Justification = "TODO: Review")]
        public StatusCommit StatusCommit { get; set; } = default!;
    }
}