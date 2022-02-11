using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace BuildBot.ServiceModel.Octopus;

public sealed class DeploymentEvent
{
    [JsonPropertyName("Id")]
    public string Id { get; set; } = default!;

    [SuppressMessage(category: "ReSharper", checkId: "AutoPropertyCanBeMadeGetOnly.Global", Justification = "TODO: Review")]
    [JsonPropertyName("Category")]
    public string Category { get; set; } = default!;

    [JsonPropertyName("UserId")]
    public string UserId { get; set; } = default!;

    [JsonPropertyName("Username")]
    public string Username { get; set; } = default!;

    [JsonPropertyName("IsService")]
    public bool IsService { get; set; }

    [JsonPropertyName("IdentityEstablishedWith")]
    public string IdentityEstablishedWith { get; set; } = default!;

    [JsonPropertyName("UserAgent")]
    public string UserAgent { get; set; } = default!;

    [JsonPropertyName("Occurred")]
    public DateTime Occurred { get; set; }

    [JsonPropertyName("Message")]
    public string Message { get; set; } = default!;

    [JsonPropertyName("MessageHtml")]
    public string MessageHtml { get; set; } = default!;

    [JsonPropertyName("MessageReferences")]
    public DeployMessageReference[] MessageReferences { get; set; } = default!;

    [JsonPropertyName("Comments")]
    public string? Comments { get; set; }

    [JsonPropertyName("Details")]
    public string? Details { get; set; }

    [SuppressMessage(category: "ReSharper", checkId: "AutoPropertyCanBeMadeGetOnly.Global", Justification = "TODO: Review")]
    [JsonPropertyName("SpaceId")]
    public string SpaceId { get; set; } = default!;

    [SuppressMessage(category: "ReSharper", checkId: "AutoPropertyCanBeMadeGetOnly.Global", Justification = "TODO: Review")]
    [JsonPropertyName("RelatedDocumentIds")]
    public string[] RelatedDocumentIds { get; set; } = default!;
}