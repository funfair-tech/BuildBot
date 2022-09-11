using System;
using System.Text.Json.Serialization;

namespace BuildBot.ServiceModel.Octopus;

//https://octopus.com/blog/notifications-with-subscriptions-and-webhooks
[JsonSourceGenerationOptionsAttribute(GenerationMode = JsonSourceGenerationMode.Serialization)]
public sealed class Deploy
{
    [JsonConstructor]
    public Deploy(in DateTime timestamp, string eventType, DeployPayload? payload)
    {
        this.Timestamp = timestamp;
        this.EventType = eventType;
        this.Payload = payload;
    }

    [JsonPropertyName("Timestamp")]
    public DateTime Timestamp { get; }

    [JsonPropertyName("EventType")]
    public string EventType { get; }

    [JsonPropertyName("Payload")]
    public DeployPayload? Payload { get; }
}