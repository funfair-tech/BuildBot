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

        //"Category": "DeploymentStarted",
        public string Category { get; set; }

        // "UserId": "users-system",
        public string UserId { get; set; }

        // "Username": "system",
        public string Username { get; set; }

        // "IsService": false,
        public bool IsService { get; set; }

        // "IdentityEstablishedWith": "",
        public string IdentityEstablishedWith { get; set; }

        // "UserAgent": "Server",
        public string UserAgent { get; set; }

        // "Occurred": "2019-04-26T18:37:34.3616214+00:00",
        public DateTime Occurred { get; set; }

        // "Message": "Deploy to Prod (#3) started  for Accounting Database release 10.33.210 to Prod",
        public string Message { get; set; }

        // "MessageHtml": "<a href='#/deployments/Deployments-15970'>Deploy to Prod (#3)</a> started  for <a href='#/projects/Projects-670'>Accounting Database</a> release <a href='#/releases/Releases-6856'>10.33.210</a> to <a href='#/environments/Environments-246'>Prod</a>",
        public string MessageHtml { get; set; }

        public DeployMessageReference[] MessageReferences { get; set; }

        // "Comments": null,
        public string Comments { get; set; }

        // "Details": null,
        public string Details { get; set; }

        // "SpaceId": "Spaces-83",
        public string SpaceId { get; set; }

        // "Links": {
        //     "Self": {}
        // }
    }

    public class DeployMessageReference
    {
        // "ReferencedDocumentId": "Deployments-15970",
        public string ReferencedDocumentId { get; set; }

        // "StartIndex": 0,
        public int StartIndex { get; set; }

        // "Length": 19
        public int Length { get; set; }
    }
}