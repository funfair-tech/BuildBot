using System;
using System.Collections.Generic;
using System.Diagnostics;
using Mediator;

namespace BuildBot.CloudFormation.Models;

[DebuggerDisplay("{TopicArn}: Id: {MessageId} @ {Timestamp}")]
public readonly record struct CloudFormationMessageReceived(string TopicArn, string MessageId, Dictionary<string, string> Properties, DateTime Timestamp) : INotification;