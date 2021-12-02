using System;
using System.Diagnostics.CodeAnalysis;

namespace BuildBot.ServiceModel.Octopus;

//https://octopus.com/blog/notifications-with-subscriptions-and-webhooks
public sealed class Deploy
{
    public DateTime Timestamp { get; set; }

    public string EventType { get; set; } = default!;

    [SuppressMessage(category: "ReSharper", checkId: "AutoPropertyCanBeMadeGetOnly.Global", Justification = "TODO: Review")]
    public DeployPayload Payload { get; set; } = default!;
}