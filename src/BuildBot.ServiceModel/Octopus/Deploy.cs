using System;

namespace BuildBot.ServiceModel.Octopus
{
    //https://octopus.com/blog/notifications-with-subscriptions-and-webhooks
    public sealed class Deploy
    {
        public DateTime Timestamp { get; set; }

        public string EventType { get; set; } = default!;

        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global
        public DeployPayload Payload { get; set; } = default!;
    }
}