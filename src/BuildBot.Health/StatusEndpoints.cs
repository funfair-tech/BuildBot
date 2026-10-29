using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BuildBot.Health.LoggingExtensions;
using BuildBot.Json.Models;
using BuildBot.ServiceModel.ComponentStatus;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BuildBot.Health;

public static class StatusEndpoints
{
    private static readonly PongDto Pong = new("Pong!");

    public static WebApplication MapStatusEndpoints(this WebApplication app)
    {
        Console.WriteLine("Configuring Test/Ping Endpoint");

        app.MapGet(pattern: "/ping", handler: PingAsync);

        return app;
    }

    public static async Task<IResult> PingAsync(
        [FromQuery] string? source,
        IMediator mediator,
        ILogger<StatusEndpointContext> logger,
        CancellationToken cancellationToken
    )
    {
        LogPing(source: source, logger: logger);

        IReadOnlyList<ServiceStatus> status = await mediator.Send(new CheckStatus(source), cancellationToken);

        return status.All(static s => s.Ok) ? Results.Ok(Pong) : Results.Conflict(status);
    }

    private static void LogPing(string? source, ILogger<StatusEndpointContext> logger)
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
