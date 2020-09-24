using System;

namespace BuildBot.ServiceModel.Octopus
{
    public sealed class DeploymentEvent
    {
        public string Id { get; set; } = default!;

        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global
        public string Category { get; set; } = default!;

        public string UserId { get; set; } = default!;

        public string Username { get; set; } = default!;

        public bool IsService { get; set; }

        public string IdentityEstablishedWith { get; set; } = default!;

        public string UserAgent { get; set; } = default!;

        public DateTime Occurred { get; set; }

        public string Message { get; set; } = default!;

        public string MessageHtml { get; set; } = default!;

        public DeployMessageReference[] MessageReferences { get; set; } = default!;

        public string Comments { get; set; } = default!;

        public string Details { get; set; } = default!;

        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global
        public string SpaceId { get; set; } = default!;

        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global
        public string[] RelatedDocumentIds { get; set; } = default!;
    }
}