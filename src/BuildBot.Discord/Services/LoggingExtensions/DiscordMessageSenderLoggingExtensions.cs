using System;
using Microsoft.Extensions.Logging;

namespace BuildBot.Discord.Services.LoggingExtensions;

internal static partial class DiscordMessageSenderLoggingExtensions
{
    [LoggerMessage(EventId = 7, Level = LogLevel.Error, Message = "DISCORD: Failed to publish message {title}")]
    public static partial void LogPublishFailed(
        this ILogger<DiscordMessageSender> logger,
        string title,
        Exception exception
    );
}
