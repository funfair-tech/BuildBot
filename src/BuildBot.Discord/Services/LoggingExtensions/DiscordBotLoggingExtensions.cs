using System.Net;
using Microsoft.Extensions.Logging;

namespace BuildBot.Discord.Services.LoggingExtensions;

internal static partial class DiscordBotLoggingExtensions
{
    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "DISCORD: Sending message {title}")]
    public static partial void LogSendingMessage(this ILogger<DiscordBot> logger, string title);

    [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "DISCORD: Sent message {title}")]
    public static partial void LogSentMessage(this ILogger<DiscordBot> logger, string title);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Error,
        Message = "DISCORD: Failed to send message {title}: HTTP {statusCode}: {responseBody}"
    )]
    public static partial void LogSendRejected(
        this ILogger<DiscordBot> logger,
        string title,
        HttpStatusCode statusCode,
        string responseBody
    );
}
