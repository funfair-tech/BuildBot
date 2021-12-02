using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace BuildBot.ServiceModel.GitHub;

public sealed class Branch
{
    [JsonPropertyName(name: "name")]
    [SuppressMessage(category: "ReSharper", checkId: "RedundantDefaultMemberInitializer", Justification = "TODO: Review")]
    public string Name { get; set; } = default!;
}