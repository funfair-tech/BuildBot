using System;
using System.Diagnostics.CodeAnalysis;

namespace BuildBot.ServiceModel.Octopus
{
#nullable disable
    public sealed class DeployPayload
    {
        [SuppressMessage(category: "Microsoft.Design", checkId: "CA1056:UriPropertiesShouldNotBeStrings", Justification = "Model for serialization")]
        public string ServerUri { get; set; }

        [SuppressMessage(category: "Microsoft.Design", checkId: "CA1056:UriPropertiesShouldNotBeStrings", Justification = "Model for serialization")]
        public string ServerAuditUri { get; set; }

        public DateTime BatchProcessingDate { get; set; }

        public DeploymentPayloadSubscription Subscription { get; set; }

        public DeploymentEvent Event { get; set; }
    }
}