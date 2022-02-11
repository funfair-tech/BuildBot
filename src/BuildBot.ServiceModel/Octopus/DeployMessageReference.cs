using System.Text.Json.Serialization;

namespace BuildBot.ServiceModel.Octopus;

public sealed class DeployMessageReference
{
    [JsonPropertyName("ReferencedDocumentId")]
    public string ReferencedDocumentId { get; init; } = default!;

    [JsonPropertyName("StartIndex")]
    public int StartIndex { get; init; }

    [JsonPropertyName("Length")]
    public int Length { get; init; }
}