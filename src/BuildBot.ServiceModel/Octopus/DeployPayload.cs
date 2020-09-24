using System;
using System.Diagnostics.CodeAnalysis;

namespace BuildBot.ServiceModel.Octopus
{
    public sealed class DeployPayload
    {
        [SuppressMessage(category: "Microsoft.Design", checkId: "CA1056:UriPropertiesShouldNotBeStrings", Justification = "Model for serialization")]

        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global
        public string ServerUri { get; set; } = default!;

        [SuppressMessage(category: "Microsoft.Design", checkId: "CA1056:UriPropertiesShouldNotBeStrings", Justification = "Model for serialization")]
        public string ServerAuditUri { get; set; } = default!;

        public DateTime BatchProcessingDate { get; set; }

        public DeploymentPayloadSubscription Subscription { get; set; } = default!;

        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global
        public DeploymentEvent Event { get; set; } = default!;
    }
}