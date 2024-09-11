using BuildBot.Models;
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
                   handler: static ([FromQuery] string? source, Logger<PongDto> logger) =>
                            {
                                LogPing(source: source, logger: logger);

                                return Results.Ok(PingPong.Model);
                            });

        return app;
    }

    private static void LogPing(string? source, Logger<PongDto> logger)
    {
        if (!string.IsNullOrWhiteSpace(source))
        {
            logger.LogError(message: "PING: Source: {Source}", source);
        }
        else
        {
            logger.LogError("PING: NO Source");
        }
    }
}