using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace BuildBot.ServiceModel.GitHub;

public sealed class StatusCommit
{
    [JsonPropertyName(name: "commit")]
    [SuppressMessage(category: "ReSharper", checkId: "RedundantDefaultMemberInitializer", Justification = "TODO: Review")]
    public Commit Commit { get; set; } = default!;

    [JsonPropertyName(name: "sha")]
    [SuppressMessage(category: "ReSharper", checkId: "RedundantDefaultMemberInitializer", Justification = "TODO: Review")]
    public string Sha { get; set; } = default!;

    [JsonPropertyName(name: "author")]
    [SuppressMessage(category: "ReSharper", checkId: "RedundantDefaultMemberInitializer", Justification = "TODO: Review")]
    public Author Author { get; set; } = default!;
}