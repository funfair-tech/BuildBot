using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace BuildBot.Octopus.Publishers.LoggingExtensions;

internal static partial class OctopusDeployNotificationHandlerLoggingExtensions
{
    [Conditional("DEBUG")]
    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Octopus: [{eventType}]")]
    public static partial void EventType(this ILogger<OctopusDeployNotificationHandler> logger, string eventType);

    [Conditional("DEBUG")]
    [LoggerMessage(EventId = 2, Level = LogLevel.Debug, Message = "{projectName}: {releaseVersion} Build successful: {succeeded} NoteWorthy: {releaseNoteWorthy}")]
    public static partial void BuildCompleted(this ILogger<OctopusDeployNotificationHandler> logger, string projectName, string releaseVersion, bool succeeded, bool releaseNoteWorthy);

    [LoggerMessage(EventId = 3, Level = LogLevel.Error, Message = "Failed to get tenant {tenantId}: {message}")]
    public static partial void FailedToGetTenant(this ILogger<OctopusDeployNotificationHandler> logger, string tenantId, string message, Exception exception);

    [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Failed to get environment {environmentId}: {message}")]
    public static partial void FailedToGetEnvironment(this ILogger<OctopusDeployNotificationHandler> logger, string environmentId, string message, Exception exception);

    [LoggerMessage(EventId = 5, Level = LogLevel.Error, Message = "Failed to get release {releaseId}: {message}")]
    public static partial void FailedToGetRelease(this ILogger<OctopusDeployNotificationHandler> logger, string releaseId, string message, Exception exception);

    [LoggerMessage(EventId = 6, Level = LogLevel.Error, Message = "Failed to get project {projectId}: {message}")]
    public static partial void FailedToGetProject(this ILogger<OctopusDeployNotificationHandler> logger, string projectId, string message, Exception exception);
}