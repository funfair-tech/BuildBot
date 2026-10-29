using Microsoft.Extensions.Logging;

namespace BuildBot.Health.LoggingExtensions;

internal static partial class StatusEndpointContextLoggingExtensions
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "PING: No Source")]
    public static partial void LogPing(this ILogger<StatusEndpointContext> logger);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "PING: Source: {Source}")]
    public static partial void LogPing(this ILogger<StatusEndpointContext> logger, string source);
}
