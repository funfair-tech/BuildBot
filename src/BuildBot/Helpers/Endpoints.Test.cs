using BuildBot.Helpers.LoggingExtensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BuildBot.Helpers;

internal static partial class Endpoints
{
    private static WebApplication ConfigureTestEndpoints(this WebApplication app)
    {
        app.MapGet(pattern: "/ping",
                   handler: static ([FromQuery] string? source, ILogger<TestEndpointContext> logger) =>
                            {
                                LogPing(source: source, logger: logger);

                                return Results.Ok(PingPong.Model);
                            });

        return app;
    }

    private static void LogPing(string? source, ILogger<TestEndpointContext> logger)
    {
        if (!string.IsNullOrWhiteSpace(source))
        {
            logger.LogPing(source);
        }
        else
        {
            logger.LogPing();
        }
    }
}