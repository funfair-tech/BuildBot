using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace BuildBot.ServiceModel.Octopus;

public sealed class DeployPayload
{
    [SuppressMessage(category: "Microsoft.Design", checkId: "CA1056:UriPropertiesShouldNotBeStrings", Justification = "Model for serialization")]
    [SuppressMessage(category: "ReSharper", checkId: "AutoPropertyCanBeMadeGetOnly.Global", Justification = "TODO: Review")]
    [JsonPropertyName("ServerUri")]
    public string ServerUri { get; init; } = default!;

    [SuppressMessage(category: "Microsoft.Design", checkId: "CA1056:UriPropertiesShouldNotBeStrings", Justification = "Model for serialization")]
    [JsonPropertyName("ServerAuditUri")]
    public string ServerAuditUri { get; init; } = default!;

    [JsonPropertyName("BatchProcessingDate")]
    public DateTime BatchProcessingDate { get; init; }

    [JsonPropertyName("Subscription")]
    public DeploymentPayloadSubscription? Subscription { get; init; }

    [SuppressMessage(category: "ReSharper", checkId: "AutoPropertyCanBeMadeGetOnly.Global", Justification = "TODO: Review")]
    [JsonPropertyName("Event")]
    public DeploymentEvent? Event { get; init; } = default!;
}