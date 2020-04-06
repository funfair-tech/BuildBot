using System;
using System.Diagnostics.CodeAnalysis;

namespace BuildBot.ServiceModel.Octopus
{
#nullable disable

    //https://octopus.com/blog/notifications-with-subscriptions-and-webhooks
    public class Deploy
    {
        public DateTime Timestamp { get; set; }

        public string EventType { get; set; }

        public DeployPayload Payload { get; set; }
    }

    public class DeployPayload
    {
        [SuppressMessage(category: "Microsoft.Design", checkId: "CA1056:UriPropertiesShouldNotBeStrings", Justification = "Model for serialization")]
        public string ServerUri { get; set; }

        [SuppressMessage(category: "Microsoft.Design", checkId: "CA1056:UriPropertiesShouldNotBeStrings", Justification = "Model for serialization")]
        public string ServerAuditUri { get; set; }

        public DateTime BatchProcessingDate { get; set; }

        public DeploymentPayloadSubscription Subscription { get; set; }

        public DeploymentEvent Event { get; set; }
    }

    public class DeploymentPayloadSubscription
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public int Type { get; set; }

        public bool IsDisabled { get; set; }
    }

    public class DeploymentEvent
    {
        public string Id { get; set; }

        public string Category { get; set; }

        public string UserId { get; set; }

        public string Username { get; set; }

        public bool IsService { get; set; }

        public string IdentityEstablishedWith { get; set; }

        public string UserAgent { get; set; }

        public DateTime Occurred { get; set; }

        public string Message { get; set; }

        public string MessageHtml { get; set; }

        public DeployMessageReference[] MessageReferences { get; set; }

        public string Comments { get; set; }

        public string Details { get; set; }

        public string SpaceId { get; set; }
    }

    public class DeployMessageReference
    {
        public string ReferencedDocumentId { get; set; }

        public int StartIndex { get; set; }

        public int Length { get; set; }
    }
}