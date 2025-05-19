using Microsoft.Extensions.Logging;

namespace BuildBot.GitHub.Publishers.LoggingExtensions;

internal static partial class GithubPingNotificationHandlerLoggingExtensions
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Github: Ping: [{hookId}] {zen}")]
    public static partial void HookZen(this ILogger<GithubPingNotificationHandler> logger, string hookId, string zen);
}