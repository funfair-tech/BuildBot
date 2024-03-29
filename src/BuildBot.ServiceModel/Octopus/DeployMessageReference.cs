﻿using System.Diagnostics;
using System.Text.Json.Serialization;

namespace BuildBot.ServiceModel.Octopus;

[DebuggerDisplay("{ReferencedDocumentId}")]
public readonly record struct DeployMessageReference
{
    [JsonConstructor]
    public DeployMessageReference(string referencedDocumentId, int startIndex, int length)
    {
        this.ReferencedDocumentId = referencedDocumentId;
        this.StartIndex = startIndex;
        this.Length = length;
    }

    [JsonPropertyName("ReferencedDocumentId")]
    public string ReferencedDocumentId { get; }

    [JsonPropertyName("StartIndex")]
    public int StartIndex { get; }

    [JsonPropertyName("Length")]
    public int Length { get; }
}