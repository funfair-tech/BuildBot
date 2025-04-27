using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace BuildBot.GitHub.Publishers.LoggingExtensions;

internal static partial class GithubPingNotificationHandlerLoggingExtensions
{
    [Conditional("DEBUG")]
    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Github: [{hookId}] {zen}")]
    public static partial void HookZen(this ILogger<GithubPingNotificationHandler> logger, string hookId, string zen);
}
