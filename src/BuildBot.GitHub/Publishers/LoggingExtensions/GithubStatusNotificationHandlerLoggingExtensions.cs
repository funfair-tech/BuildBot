using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace BuildBot.GitHub.Publishers.LoggingExtensions;

internal static partial class GithubStatusNotificationHandlerLoggingExtensions
{
    [Conditional("DEBUG")]
    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Github: [{githubRef}]")]
    public static partial void GitHubRef(
        this ILogger<GithubStatusNotificationHandler> logger,
        string githubRef
    );
}
