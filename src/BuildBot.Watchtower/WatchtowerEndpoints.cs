using System;
using System.Threading;
using System.Threading.Tasks;
using BuildBot.ServiceModel.Watchtower;
using BuildBot.Watchtower.Models;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace BuildBot.Watchtower;

public static class WatchtowerEndpoints
{
    public static WebApplication MapWatchtowerEndpoints(this WebApplication app)
    {
        Console.WriteLine("Configuring Watchtower Endpoint");

        RouteGroupBuilder group = app.MapGroup("/watchtower");
        group.MapPost(pattern: "deploy", handler: DeployAsync);

        return app;
    }

    public static async Task<IResult> DeployAsync(
        WatchTowerMessage model,
        IMediator mediator,
        CancellationToken cancellationToken
    )
    {
        await mediator.Publish(new WatchTowerPublishMessage(model), cancellationToken: cancellationToken);

        return Results.NoContent();
    }
}
