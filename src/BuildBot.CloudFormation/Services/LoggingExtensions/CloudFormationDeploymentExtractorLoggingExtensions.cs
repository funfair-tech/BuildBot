using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace BuildBot.CloudFormation.Services.LoggingExtensions;

internal static partial class CloudFormationDeploymentExtractorLoggingExtensions
{
    [LoggerMessage(eventId: 0, level: LogLevel.Warning, message: "CLOUDFORMATION: Not a cloud formation stack")]
    public static partial void NotACloudFormationStack(this ILogger<CloudFormationDeploymentExtractor> logger);

    [LoggerMessage(eventId: 1, level: LogLevel.Warning, message: "CLOUDFORMATION: No stack id found")]
    public static partial void NoStackIdFound(this ILogger<CloudFormationDeploymentExtractor> logger);

    [LoggerMessage(eventId: 2, level: LogLevel.Warning, message: "CLOUDFORMATION: No stack name found")]
    public static partial void NoStackNameFound(this ILogger<CloudFormationDeploymentExtractor> logger);

    [LoggerMessage(eventId: 3, level: LogLevel.Warning, message: "CLOUDFORMATION: No logical resource id found")]
    public static partial void NoLogicalResourceIdFound(this ILogger<CloudFormationDeploymentExtractor> logger);

    [LoggerMessage(
        eventId: 4,
        level: LogLevel.Information,
        message: "CLOUDFORMATION: Logical resource id: {logicalResourceId}"
    )]
    public static partial void LogicalResourceIdFound(
        this ILogger<CloudFormationDeploymentExtractor> logger,
        string logicalResourceId
    );

    [LoggerMessage(eventId: 5, level: LogLevel.Warning, message: "CLOUDFORMATION: No physical resource id found")]
    public static partial void NoPhysicalResourceIdFound(this ILogger<CloudFormationDeploymentExtractor> logger);

    [LoggerMessage(eventId: 6, level: LogLevel.Warning, message: "CLOUDFORMATION: No resource status found")]
    public static partial void NoResourceStatusFound(this ILogger<CloudFormationDeploymentExtractor> logger);

    [LoggerMessage(
        eventId: 7,
        level: LogLevel.Information,
        message: "CLOUDFORMATION: Unknown resource status: {status}"
    )]
    public static partial void UnknownStatus(this ILogger<CloudFormationDeploymentExtractor> logger, string status);

    [LoggerMessage(eventId: 8, level: LogLevel.Information, message: "CLOUDFORMATION: Stack Name: {stackName}")]
    public static partial void StackName(this ILogger<CloudFormationDeploymentExtractor> logger, string stackName);

    [LoggerMessage(eventId: 9, level: LogLevel.Information, message: "CLOUDFORMATION: Project: {projectName}")]
    public static partial void Project(this ILogger<CloudFormationDeploymentExtractor> logger, string projectName);

    [LoggerMessage(eventId: 10, level: LogLevel.Information, message: "CLOUDFORMATION: ARN: {arn}")]
    public static partial void AwsArn(this ILogger<CloudFormationDeploymentExtractor> logger, string arn);

    [LoggerMessage(
        eventId: 11,
        level: LogLevel.Information,
        message: "CLOUDFORMATION: Resource Status: {status} : Build Succeeded: {success}"
    )]
    public static partial void ResourceStatus(
        this ILogger<CloudFormationDeploymentExtractor> logger,
        string status,
        bool success
    );

    [Conditional("DEBUG")]
    [LoggerMessage(eventId: 12, level: LogLevel.Debug, message: "CLOUDFORMATION: Property {key} = \"{value}\"")]
    public static partial void Property(
        this ILogger<CloudFormationDeploymentExtractor> logger,
        string key,
        string value
    );
}
