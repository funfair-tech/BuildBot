using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace BuildBot.ServiceModel.Octopus;

public sealed class DeploymentEvent
{
    [JsonPropertyName("Id")]
    public string Id { get; init; } = default!;

    [SuppressMessage(category: "ReSharper", checkId: "AutoPropertyCanBeMadeGetOnly.Global", Justification = "TODO: Review")]
    [JsonPropertyName("Category")]
    public string Category { get; init; } = default!;

    [JsonPropertyName("UserId")]
    public string UserId { get; init; } = default!;

    [JsonPropertyName("Username")]
    public string Username { get; init; } = default!;

    [JsonPropertyName("IsService")]
    public bool IsService { get; init; }

    [JsonPropertyName("IdentityEstablishedWith")]
    public string IdentityEstablishedWith { get; init; } = default!;

    [JsonPropertyName("UserAgent")]
    public string UserAgent { get; init; } = default!;

    [JsonPropertyName("Occurred")]
    public DateTime Occurred { get; init; }

    [JsonPropertyName("Message")]
    public string Message { get; init; } = default!;

    [JsonPropertyName("MessageHtml")]
    public string MessageHtml { get; init; } = default!;

    [JsonPropertyName("MessageReferences")]
    public DeployMessageReference[] MessageReferences { get; init; } = default!;

    [JsonPropertyName("Comments")]
    public string? Comments { get; init; }

    [JsonPropertyName("Details")]
    public string? Details { get; init; }

    [SuppressMessage(category: "ReSharper", checkId: "AutoPropertyCanBeMadeGetOnly.Global", Justification = "TODO: Review")]
    [JsonPropertyName("SpaceId")]
    public string SpaceId { get; init; } = default!;

    [SuppressMessage(category: "ReSharper", checkId: "AutoPropertyCanBeMadeGetOnly.Global", Justification = "TODO: Review")]
    [JsonPropertyName("RelatedDocumentIds")]
    public string[] RelatedDocumentIds { get; init; } = default!;
}