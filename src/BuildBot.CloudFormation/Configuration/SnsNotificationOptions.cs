using System;
using System.Diagnostics;

namespace BuildBot.CloudFormation.Configuration;

[DebuggerDisplay("SNS Topic: {TopicArn}")]
public readonly record struct SnsNotificationOptions(string TopicArn)
{
    public bool IsValidArn(string notificationTopicArn)
    {
        return StringComparer.Ordinal.Equals(x: notificationTopicArn, y: this.TopicArn);
    }
}