using Microsoft.Extensions.Logging;

namespace BuildBot.Discord.Services.LoggingExtensions;

internal static partial class DiscordMessageSenderLoggingExtensions
{
    // Only the exception type is logged: an exception message can carry the request URL, which embeds the webhook token.
    [LoggerMessage(
        EventId = 7,
        Level = LogLevel.Error,
        Message = "DISCORD: Failed to publish message {title} with {exceptionType}"
    )]
    public static partial void LogPublishFailed(
        this ILogger<DiscordMessageSender> logger,
        string title,
        string exceptionType
    );
}
