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
}
