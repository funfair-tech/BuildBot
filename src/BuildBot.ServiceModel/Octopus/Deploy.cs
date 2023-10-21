using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace BuildBot.ServiceModel.Octopus;

//https://octopus.com/blog/notifications-with-subscriptions-and-webhooks
[DebuggerDisplay("{EventType}")]
public readonly record struct Deploy
{
    [SuppressMessage(category: "Roslynator.Analyzers", checkId: "RCS1231: Make parameter ref read-only.", Justification = "Serialisation model")]
    [JsonConstructor]
    public Deploy(DateTime timestamp, string eventType, DeployPayload? payload)
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