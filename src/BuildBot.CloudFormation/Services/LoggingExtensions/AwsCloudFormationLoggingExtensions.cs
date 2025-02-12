using System;
using Microsoft.Extensions.Logging;

namespace BuildBot.CloudFormation.Services.LoggingExtensions;

internal static partial class AwsCloudFormationLoggingExtensions
{
    [LoggerMessage(eventId: 0, level: LogLevel.Warning, message: "Failed to get cloudformation stack: {message}")]
    public static partial void FailedToGetCloudFormationStack(this ILogger<AwsCloudFormation> logger, string message, Exception exception);
}
