using System;
using System.Diagnostics;
using Mediator;

namespace BuildBot.CloudFormation.Models;

[DebuggerDisplay("{TopicArn}: {SubscribeUrl}")]
public readonly record struct CloudFormationSubscriptionConfirmation(string TopicArn, Uri SubscribeUrl) : INotification;
