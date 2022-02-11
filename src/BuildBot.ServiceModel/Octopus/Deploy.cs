using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace BuildBot.ServiceModel.Octopus;

//https://octopus.com/blog/notifications-with-subscriptions-and-webhooks
[JsonSourceGenerationOptionsAttribute(GenerationMode = JsonSourceGenerationMode.Serialization)]
public sealed class Deploy
{
    [JsonPropertyName("Timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonPropertyName("EventType")]
    public string EventType { get; set; } = default!;

    [SuppressMessage(category: "ReSharper", checkId: "UnusedAutoPropertyAccessor.Global", Justification = "TODO: Review")]
    [JsonPropertyName("Payload")]
    public DeployPayload? Payload { get; set; }
}