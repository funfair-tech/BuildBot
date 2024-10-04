using System.Diagnostics;

namespace BuildBot.CloudFormation.Configuration;

[DebuggerDisplay("SNS Topic: {TopicArn}")]
public readonly record struct SnsNotificationOptions(string TopicArn);