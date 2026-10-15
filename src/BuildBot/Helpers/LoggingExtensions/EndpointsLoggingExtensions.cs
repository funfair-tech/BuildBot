using Microsoft.Extensions.Logging;

namespace BuildBot.Helpers.LoggingExtensions;

internal static partial class EndpointsLoggingExtensions
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Warning,
        Message = "Github: Webhook secret is not configured; webhook signatures will not be verified"
    )]
    public static partial void GitHubWebhookSecretNotConfigured(this ILogger logger);
}
