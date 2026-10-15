using Microsoft.Extensions.Logging;

namespace BuildBot.GitHub.Filters.LoggingExtensions;

internal static partial class GitHubWebhookSignatureFilterLoggingExtensions
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Warning,
        Message = "Github: Rejected webhook to {Path}: missing or invalid signature"
    )]
    public static partial void WebhookSignatureRejected(this ILogger<GitHubWebhookSignatureFilter> logger, string path);
}
