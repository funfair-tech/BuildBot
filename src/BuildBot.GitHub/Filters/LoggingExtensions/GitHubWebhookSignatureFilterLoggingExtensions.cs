using Microsoft.Extensions.Logging;

namespace BuildBot.GitHub.Filters.LoggingExtensions;

internal static partial class GitHubWebhookSignatureFilterLoggingExtensions
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Warning,
        Message = "Github: Rejected webhook to {Path}: {Reason} (delivery: {Delivery}, event: {GitHubEvent}, remote address: {RemoteIpAddress}, forwarded for: {ForwardedFor})"
    )]
    public static partial void WebhookSignatureRejected(
        this ILogger<GitHubWebhookSignatureFilter> logger,
        string path,
        WebhookSignatureVerificationResult reason,
        string delivery,
        string gitHubEvent,
        string remoteIpAddress,
        string forwardedFor
    );
}
