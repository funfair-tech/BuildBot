using System;
using System.Diagnostics.CodeAnalysis;

namespace BuildBot.ServiceModel.Octopus;

//https://octopus.com/blog/notifications-with-subscriptions-and-webhooks
public sealed class Deploy
{
    public DateTime Timestamp { get; init; }

    public string EventType { get; init; } = default!;

    [SuppressMessage(category: "ReSharper", checkId: "UnusedAutoPropertyAccessor.Global", Justification = "TODO: Review")]
    public DeployPayload? Payload { get; init; }
}