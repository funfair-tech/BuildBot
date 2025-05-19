using Microsoft.Extensions.Logging;

namespace BuildBot.GitHub.Publishers.LoggingExtensions;

internal static partial class GithubStatusNotificationHandlerLoggingExtensions
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Github: Status [{githubRef}]")]
    public static partial void GitHubRef(this ILogger<GithubStatusNotificationHandler> logger, string githubRef);
}
