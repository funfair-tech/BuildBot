using System;
using System.Diagnostics.CodeAnalysis;

namespace BuildBot.ServiceModel.Octopus;

public sealed class DeployPayload
{
    [SuppressMessage(category: "Microsoft.Design", checkId: "CA1056:UriPropertiesShouldNotBeStrings", Justification = "Model for serialization")]
    [SuppressMessage(category: "ReSharper", checkId: "AutoPropertyCanBeMadeGetOnly.Global", Justification = "TODO: Review")]
    public string ServerUri { get; set; } = default!;

    [SuppressMessage(category: "Microsoft.Design", checkId: "CA1056:UriPropertiesShouldNotBeStrings", Justification = "Model for serialization")]
    public string ServerAuditUri { get; set; } = default!;

    public DateTime BatchProcessingDate { get; set; }

    public DeploymentPayloadSubscription Subscription { get; set; } = default!;

    [SuppressMessage(category: "ReSharper", checkId: "AutoPropertyCanBeMadeGetOnly.Global", Justification = "TODO: Review")]
    public DeploymentEvent Event { get; set; } = default!;
}