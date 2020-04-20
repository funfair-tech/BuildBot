using System;

namespace BuildBot.ServiceModel.Octopus
{
#nullable disable
    public sealed class DeploymentEvent
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

        public string[] RelatedDocumentIds { get; set; }
    }
}