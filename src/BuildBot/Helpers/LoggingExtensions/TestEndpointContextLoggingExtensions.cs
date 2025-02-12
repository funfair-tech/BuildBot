using Microsoft.Extensions.Logging;

namespace BuildBot.Helpers.LoggingExtensions;

internal static partial class TestEndpointContextLoggingExtensions
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "PING: No Source")]
    public static partial void LogPing(this ILogger<TestEndpointContext> logger);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "PING: Source: {Source}")]
    public static partial void LogPing(this ILogger<TestEndpointContext> logger, string source);
}
