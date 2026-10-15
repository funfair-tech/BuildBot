using Microsoft.Extensions.Logging;

namespace BuildBot.Discord.Services.LoggingExtensions;

internal static partial class DiscordWebhookClientLoggingExtensions
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Warning,
        Message = "DISCORD: Rate limited on attempt {attempt}: retrying in {delaySeconds} seconds"
    )]
    public static partial void LogRateLimited(
        this ILogger<DiscordWebhookClient> logger,
        int attempt,
        double delaySeconds
    );
}
