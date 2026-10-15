using Microsoft.Extensions.Logging;

namespace BuildBot.GitHub.Middleware.LoggingExtensions;

internal static partial class GitHubWebhookSignatureMiddlewareLoggingExtensions
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Warning,
        Message = "Github: Rejected webhook to {Path}: {Reason} (delivery: {Delivery}, event: {GitHubEvent}, remote address: {RemoteIpAddress}, forwarded for: {ForwardedFor})"
    )]
    public static partial void WebhookSignatureRejected(
        this ILogger<GitHubWebhookSignatureMiddleware> logger,
        string path,
        WebhookSignatureVerificationResult reason,
        string delivery,
        string gitHubEvent,
        string remoteIpAddress,
        string forwardedFor
    );
}
