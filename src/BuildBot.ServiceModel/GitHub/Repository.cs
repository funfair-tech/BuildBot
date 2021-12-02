using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace BuildBot.ServiceModel.GitHub;

public sealed class Repository
{
    [JsonPropertyName(name: "id")]
    public int Id { get; set; }

    [JsonPropertyName(name: "name")]
    [SuppressMessage(category: "ReSharper", checkId: "RedundantDefaultMemberInitializer", Justification = "TODO: Review")]
    public string Name { get; set; } = default!;

    [JsonPropertyName(name: "full_name")]
    [SuppressMessage(category: "ReSharper", checkId: "RedundantDefaultMemberInitializer", Justification = "TODO: Review")]
    public string FullName { get; set; } = default!;

    [JsonPropertyName(name: "owner")]
    [SuppressMessage(category: "ReSharper", checkId: "RedundantDefaultMemberInitializer", Justification = "TODO: Review")]
    public Owner Owner { get; set; } = default!;
}