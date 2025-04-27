using Microsoft.Extensions.Logging;

namespace BuildBot.Discord.Publishers.LoggingExtensions;

internal static partial class DiscordBotMessageNotificationHandlerLoggingExtensions
{
    [LoggerMessage(eventId: 1, level: LogLevel.Information, message: "DISCORD: Queueing message: {title}")]
    public static partial void QueueingDiscordMessage(
        this ILogger<DiscordBotMessageNotificationHandler> logger,
        string title
    );
}
