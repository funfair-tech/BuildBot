using System;
using System.Threading;
using BuildBot.ServiceModel.Watchtower;
using BuildBot.Watchtower.Models;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace BuildBot.Helpers;

internal static partial class Endpoints
{
    private static WebApplication ConfigureWatchtowerEndpoints(this WebApplication app)
    {
        Console.WriteLine("Configuring Watchtower Endpoint");

        RouteGroupBuilder group = app.MapGroup("/watchtower");
        group.MapPost(pattern: "deploy",
                      handler: static async (WatchTowerMessage model, IMediator mediator, CancellationToken cancellationToken) =>
                               {
                                   await mediator.Publish(new WatchTowerPublishMessage(model), cancellationToken: cancellationToken);

                                   return Results.NoContent();
                               });

        return app;
    }
}