using Microsoft.Extensions.Logging;

namespace BuildBot.GitHub.Publishers.LoggingExtensions;

internal static partial class GithubPushNotificationHandlerLoggingExtensions
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Github: Push: [{githubRef}]")]
    public static partial void GitHubRef(this ILogger<GithubPushNotificationHandler> logger, string githubRef);
}
