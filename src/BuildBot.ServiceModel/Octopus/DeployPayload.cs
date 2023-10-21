using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace BuildBot.ServiceModel.Octopus;

[DebuggerDisplay("{ServerUri}")]
public readonly record struct DeployPayload
{
    [SuppressMessage(category: "Roslynator.Analyzers", checkId: "RCS1231: Make parameter ref read-only.", Justification = "Serialisation model")]
    [JsonConstructor]
    public DeployPayload(string serverUri, string serverAuditUri, DateTime batchProcessingDate, DeploymentPayloadSubscription? subscription, DeploymentEvent? @event)
    {
        this.ServerUri = serverUri;
        this.ServerAuditUri = serverAuditUri;
        this.BatchProcessingDate = batchProcessingDate;
        this.Subscription = subscription;
        this.Event = @event;
    }

    [SuppressMessage(category: "Microsoft.Design", checkId: "CA1056:UriPropertiesShouldNotBeStrings", Justification = "Model for serialization")]
    [JsonPropertyName("ServerUri")]
    public string ServerUri { get; }

    [SuppressMessage(category: "Microsoft.Design", checkId: "CA1056:UriPropertiesShouldNotBeStrings", Justification = "Model for serialization")]
    [JsonPropertyName("ServerAuditUri")]
    public string ServerAuditUri { get; }

    [JsonPropertyName("BatchProcessingDate")]
    public DateTime BatchProcessingDate { get; }

    [JsonPropertyName("Subscription")]
    public DeploymentPayloadSubscription? Subscription { get; }

    [JsonPropertyName("Event")]
    public DeploymentEvent? Event { get; }
}