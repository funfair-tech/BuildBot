using System;
using Microsoft.Extensions.Logging;

namespace BuildBot.CloudFormation.Publishers.LoggingExtensions;

public static partial class CloudFormationSubscriptionConfirmationNotificationHandlerLoggingExtensions
{
    [LoggerMessage(eventId: 0, level: LogLevel.Warning, message: "CLOUDFORMATION: Received Invalid ARN {arn}")]
    public static partial void ReceivedInvalidArn(this ILogger<CloudFormationSubscriptionConfirmationNotificationHandler> logger, string arn);

    [LoggerMessage(eventId: 1, level: LogLevel.Error, message: "CLOUDFORMATION: Failed to subscribe to topic {subscribeUrl} : {message}")]
    public static partial void FailedToSubscribeToTopic(this ILogger<CloudFormationSubscriptionConfirmationNotificationHandler> logger, Uri subscribeUrl, string message, Exception exception);
}