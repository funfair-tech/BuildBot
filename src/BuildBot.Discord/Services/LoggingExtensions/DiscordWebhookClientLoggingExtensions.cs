using System;
using System.Net;
using Microsoft.Extensions.Logging;

namespace BuildBot.Discord.Services.LoggingExtensions;

internal static partial class DiscordWebhookClientLoggingExtensions
{
    [LoggerMessage(
        EventId = 8,
        Level = LogLevel.Warning,
        Message = "DISCORD: Webhook attempt {attempt} failed with HTTP {statusCode}; retrying in {delay}"
    )]
    public static partial void LogRetryingAfterStatusCode(
        this ILogger<DiscordWebhookClient> logger,
        int attempt,
        HttpStatusCode statusCode,
        TimeSpan delay
    );

    // Only the exception type is logged: an exception message can carry the request URL, which embeds the webhook token.
    [LoggerMessage(
        EventId = 9,
        Level = LogLevel.Warning,
        Message = "DISCORD: Webhook attempt {attempt} failed with {exceptionType}; retrying in {delay}"
    )]
    public static partial void LogRetryingAfterException(
        this ILogger<DiscordWebhookClient> logger,
        int attempt,
        string exceptionType,
        TimeSpan delay
    );
}
