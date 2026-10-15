using System.Linq;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace BuildBot.Discord.Tests;

internal static class LoggerTestExtensions
{
    public static string LoggedText(this ILogger logger)
    {
        return string.Join(
            separator: '\n',
            values: logger
                .ReceivedCalls()
                .SelectMany(static call => call.GetArguments())
                .Select(static arg => arg?.ToString())
        );
    }
}
