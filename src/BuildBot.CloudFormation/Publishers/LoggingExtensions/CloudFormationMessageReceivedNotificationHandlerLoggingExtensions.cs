using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace BuildBot.CloudFormation.Publishers.LoggingExtensions;

public static partial class CloudFormationMessageReceivedNotificationHandlerLoggingExtensions
{
    [LoggerMessage(eventId: 0, level: LogLevel.Warning, message: "CLOUDFORMATION: Received Invalid ARN {arn}")]
    public static partial void ReceivedInvalidArn(this ILogger<CloudFormationMessageReceivedNotificationHandler> logger, string arn);

    [LoggerMessage(eventId: 1, level: LogLevel.Information, message: "CLOUDFORMATION: Building message for {project} in {stackName}")]
    private static partial void BuildingMessage(this ILogger<CloudFormationMessageReceivedNotificationHandler> logger, string project, string stackName);

    public static void BuildingMessage(this ILogger<CloudFormationMessageReceivedNotificationHandler> logger, in Deployment deployment)
    {
        logger.BuildingMessage(project: deployment.Project, stackName: deployment.StackName);
    }

    [Conditional("DEBUG")]
    [LoggerMessage(eventId: 2, level: LogLevel.Information, message: "CLOUDFORMATION: Publishing message for {project} in {stackName}")]
    private static partial void PublishingMessage(this ILogger<CloudFormationMessageReceivedNotificationHandler> logger, string project, string stackName);

    public static void PublishingMessage(this ILogger<CloudFormationMessageReceivedNotificationHandler> logger, in Deployment deployment)
    {
        logger.PublishingMessage(project: deployment.Project, stackName: deployment.StackName);
    }
}