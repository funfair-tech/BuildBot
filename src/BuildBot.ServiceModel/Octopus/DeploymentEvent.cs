using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace BuildBot.ServiceModel.Octopus;

[DebuggerDisplay("{Id}: {Message}")]
public readonly record struct DeploymentEvent
{
    [SuppressMessage(category: "Roslynator.Analyzers", checkId: "RCS1231: Make parameter ref read-only.", Justification = "Serialisation model")]
    [SuppressMessage(category: "Meziantou.Analyzer", checkId: "MA0109: Add an overload with a Span or Memory parameter", Justification = "Won't work here")]
    [JsonConstructor]
    public DeploymentEvent(string id,
                           string category,
                           string userId,
                           string username,
                           bool isService,
                           string identityEstablishedWith,
                           string userAgent,
                           DateTime occurred,
                           string message,
                           string messageHtml,
                           DeployMessageReference[] messageReferences,
                           string? comments,
                           string? details,
                           string spaceId,
                           string[] relatedDocumentIds)
    {
        this.Id = id;
        this.Category = category;
        this.UserId = userId;
        this.Username = username;
        this.IsService = isService;
        this.IdentityEstablishedWith = identityEstablishedWith;
        this.UserAgent = userAgent;
        this.Occurred = occurred;
        this.Message = message;
        this.MessageHtml = messageHtml;
        this.MessageReferences = messageReferences;
        this.Comments = comments;
        this.Details = details;
        this.SpaceId = spaceId;
        this.RelatedDocumentIds = relatedDocumentIds;
    }

    [JsonPropertyName("Id")]
    public string Id { get; }

    [JsonPropertyName("Category")]
    public string Category { get; }

    [JsonPropertyName("UserId")]
    public string UserId { get; }

    [JsonPropertyName("Username")]
    public string Username { get; }

    [JsonPropertyName("IsService")]
    public bool IsService { get; }

    [JsonPropertyName("IdentityEstablishedWith")]
    public string IdentityEstablishedWith { get; }

    [JsonPropertyName("UserAgent")]
    public string UserAgent { get; }

    [JsonPropertyName("Occurred")]
    public DateTime Occurred { get; }

    [JsonPropertyName("Message")]
    public string Message { get; }

    [JsonPropertyName("MessageHtml")]
    public string MessageHtml { get; }

    [JsonPropertyName("MessageReferences")]
    public DeployMessageReference[] MessageReferences { get; }

    [JsonPropertyName("Comments")]
    public string? Comments { get; }

    [JsonPropertyName("Details")]
    public string? Details { get; }

    [JsonPropertyName("SpaceId")]
    public string SpaceId { get; }

    [JsonPropertyName("RelatedDocumentIds")]
    public string[] RelatedDocumentIds { get; }
}