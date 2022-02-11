using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace BuildBot.ServiceModel.Octopus;

public sealed class DeployPayload
{
    [SuppressMessage(category: "Microsoft.Design", checkId: "CA1056:UriPropertiesShouldNotBeStrings", Justification = "Model for serialization")]
    [SuppressMessage(category: "ReSharper", checkId: "AutoPropertyCanBeMadeGetOnly.Global", Justification = "TODO: Review")]
    [JsonPropertyName("ServerUri")]
    public string ServerUri { get; set; } = default!;

    [SuppressMessage(category: "Microsoft.Design", checkId: "CA1056:UriPropertiesShouldNotBeStrings", Justification = "Model for serialization")]
    [JsonPropertyName("ServerAuditUri")]
    public string ServerAuditUri { get; set; } = default!;

    [JsonPropertyName("BatchProcessingDate")]
    public DateTime BatchProcessingDate { get; set; }

    [JsonPropertyName("Subscription")]
    public DeploymentPayloadSubscription? Subscription { get; set; }

    [SuppressMessage(category: "ReSharper", checkId: "AutoPropertyCanBeMadeGetOnly.Global", Justification = "TODO: Review")]
    [JsonPropertyName("Event")]
    public DeploymentEvent? Event { get; set; } = default!;
}