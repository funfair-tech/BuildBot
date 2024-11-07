using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BuildBot.Health;
using BuildBot.Helpers.LoggingExtensions;
using BuildBot.ServiceModel.ComponentStatus;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BuildBot.Helpers;

internal static partial class Endpoints
{
    private static WebApplication ConfigureTestEndpoints(this WebApplication app)
    {
        Console.WriteLine("Configuring Test/Ping Endpoint");

        app.MapGet(pattern: "/ping",
                   handler: static async ([FromQuery] string? source, IMediator mediator, ILogger<TestEndpointContext> logger, CancellationToken cancellationToken) =>
                            {
                                LogPing(source: source, logger: logger);

                                IReadOnlyList<ServiceStatus> status = await mediator.Send(new CheckStatus(source), cancellationToken);

                                return status.All(s => s.Ok)
                                    ? Results.Ok(PingPong.Model)
                                    : Results.Conflict(status);
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