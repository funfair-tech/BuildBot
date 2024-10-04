using System;
using System.Diagnostics;
using Mediator;

namespace BuildBot.CloudFormation.Models;

[DebuggerDisplay("{TopicArn}: {SubscribeURL}")]
public readonly record struct CloudFormationSubscriptionConfirmation(string TopicArn, Uri SubscribeURL) : INotification;