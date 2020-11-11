using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace BuildBot.ServiceModel.GitHub
{
    public sealed class CommitUser
    {
        [JsonPropertyName(name: "name")]
        [SuppressMessage(category: "ReSharper", checkId: "RedundantDefaultMemberInitializer", Justification = "TODO: Review")]
        public string Name { get; set; } = default!;

        [JsonPropertyName(name: "email")]
        [SuppressMessage(category: "ReSharper", checkId: "RedundantDefaultMemberInitializer", Justification = "TODO: Review")]
        public string Email { get; set; } = default!;

        [JsonPropertyName(name: "username")]
        [SuppressMessage(category: "ReSharper", checkId: "RedundantDefaultMemberInitializer", Justification = "TODO: Review")]
        public string? Username { get; set; }
    }
}