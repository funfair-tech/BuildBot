using System;

namespace BuildBot.ServiceModel.Octopus
{
#nullable disable

    //https://octopus.com/blog/notifications-with-subscriptions-and-webhooks
    public sealed class Deploy
    {
        public DateTime Timestamp { get; set; }

        public string EventType { get; set; }

        public DeployPayload Payload { get; set; }
    }
}