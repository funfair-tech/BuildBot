using System;
using Microsoft.Extensions.Logging;

namespace BuildBot.Discord.Services.LoggingExtensions;

internal static partial class DiscordBotLoggingExtensions
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Error,
        Message = "DISCORD: {channelName}: Could not find channel on server {serverName}"
    )]
    public static partial void LogDiscordChannelNotFound(
        this ILogger<DiscordBot> logger,
        string channelName,
        string serverName
    );

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Warning,
        Message = "DISCORD: {channelName}: Sending message {message}"
    )]
    public static partial void LogSendingMessage(this ILogger<DiscordBot> logger, string channelName, string message);

    [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "DISCORD: {channelName}: Sent message {message}")]
    public static partial void LogSentMessage(this ILogger<DiscordBot> logger, string channelName, string message);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Error,
        Message = "DISCORD: {channelName}: Failed to send message {title}: {message}"
    )]
    public static partial void FailedToPublishMessage(
        this ILogger<DiscordBot> logger,
        string channelName,
        string title,
        string message,
        Exception exception
    );

    [LoggerMessage(EventId = 6, Level = LogLevel.Debug, Message = "DISCORD: DEBUG: {message}")]
    public static partial void DiscordDebug(this ILogger<DiscordBot> logger, string message);

    [LoggerMessage(EventId = 7, Level = LogLevel.Information, Message = "DISCORD: INFO: {message}")]
    public static partial void DiscordInfo(this ILogger<DiscordBot> logger, string message);

    [LoggerMessage(EventId = 8, Level = LogLevel.Warning, Message = "DISCORD: WARNING: {message}")]
    public static partial void DiscordWarning(this ILogger<DiscordBot> logger, string message);

    [LoggerMessage(EventId = 9, Level = LogLevel.Error, Message = "DISCORD: ERROR: {message}")]
    public static partial void DiscordError(this ILogger<DiscordBot> logger, string message);

    [LoggerMessage(EventId = 9, Level = LogLevel.Error, Message = "DISCORD: ERROR: {message}")]
    public static partial void DiscordError(this ILogger<DiscordBot> logger, string message, Exception exception);

    [LoggerMessage(EventId = 10, Level = LogLevel.Critical, Message = "DISCORD: CRITICAL ERROR: {message}")]
    public static partial void DiscordCriticalError(this ILogger<DiscordBot> logger, string message);

    [LoggerMessage(EventId = 10, Level = LogLevel.Critical, Message = "DISCORD: CRITICAL ERROR: {message}")]
    public static partial void DiscordCriticalError(
        this ILogger<DiscordBot> logger,
        string message,
        Exception exception
    );
}
