using System.Text.Json.Serialization;

namespace BuildBot.ServiceModel.Octopus;

public sealed class DeployMessageReference
{
    [JsonPropertyName("ReferencedDocumentId")]
    public string ReferencedDocumentId { get; set; } = default!;

    [JsonPropertyName("StartIndex")]
    public int StartIndex { get; set; }

    [JsonPropertyName("Length")]
    public int Length { get; set; }
}