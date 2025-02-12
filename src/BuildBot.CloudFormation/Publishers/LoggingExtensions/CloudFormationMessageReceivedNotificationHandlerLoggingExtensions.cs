using System;
using System.Diagnostics;
using BuildBot.CloudFormation.Models;
using Microsoft.Extensions.Logging;

namespace BuildBot.CloudFormation.Publishers.LoggingExtensions;

internal static partial class CloudFormationMessageReceivedNotificationHandlerLoggingExtensions
{
    [LoggerMessage(
        eventId: 0,
        level: LogLevel.Warning,
        message: "CLOUDFORMATION: Received Invalid ARN {arn}"
    )]
    public static partial void ReceivedInvalidArn(
        this ILogger<CloudFormationMessageReceivedNotificationHandler> logger,
        string arn
    );

    [LoggerMessage(
        eventId: 1,
        level: LogLevel.Information,
        message: "CLOUDFORMATION: Building message for {project} in {stackName}"
    )]
    private static partial void BuildingMessage(
        this ILogger<CloudFormationMessageReceivedNotificationHandler> logger,
        string project,
        string stackName
    );

    public static void BuildingMessage(
        this ILogger<CloudFormationMessageReceivedNotificationHandler> logger,
        in Deployment deployment
    )
    {
        logger.BuildingMessage(project: deployment.Project, stackName: deployment.StackName);
    }

    [Conditional("DEBUG")]
    [LoggerMessage(
        eventId: 2,
        level: LogLevel.Information,
        message: "CLOUDFORMATION: Publishing message for {project} in {stackName}"
    )]
    private static partial void PublishingMessage(
        this ILogger<CloudFormationMessageReceivedNotificationHandler> logger,
        string project,
        string stackName
    );

    public static void PublishingMessage(
        this ILogger<CloudFormationMessageReceivedNotificationHandler> logger,
        in Deployment deployment
    )
    {
        logger.PublishingMessage(project: deployment.Project, stackName: deployment.StackName);
    }

    [LoggerMessage(
        eventId: 3,
        level: LogLevel.Information,
        message: "CLOUDFORMATION: Received message from {topicArn} with {messageId} at {timestamp}"
    )]
    private static partial void RecievedMessage(
        this ILogger<CloudFormationMessageReceivedNotificationHandler> logger,
        string topicArn,
        string messageId,
        DateTime timestamp
    );

    public static void RecievedMessage(
        this ILogger<CloudFormationMessageReceivedNotificationHandler> logger,
        in CloudFormationMessageReceived notification
    )
    {
        logger.RecievedMessage(
            topicArn: notification.TopicArn,
            messageId: notification.MessageId,
            timestamp: notification.Timestamp
        );
    }
}
