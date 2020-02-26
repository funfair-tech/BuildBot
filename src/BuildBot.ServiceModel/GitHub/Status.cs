using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace BuildBot.ServiceModel.GitHub
{
    public sealed class Status
    {
        [SuppressMessage(category: "Microsoft.Design", checkId: "CA1056:UriPropertiesShouldNotBeStrings", Justification = "Model for serialization")]
        [JsonPropertyName("target_url")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public string TargetUrl { get; set; } = default!;

        [JsonPropertyName("repository")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public Repository Repository { get; set; } = default!;

        [JsonPropertyName("context")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public string Context { get; set; } = default!;

        [JsonPropertyName("state")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public string State { get; set; } = default!;

        [JsonPropertyName("branches")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public List<Branch> Branches { get; set; } = default!;

        [JsonPropertyName("description")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public string Description { get; set; } = default!;

        [JsonPropertyName("commit")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public StatusCommit StatusCommit { get; set; } = default!;
    }
}