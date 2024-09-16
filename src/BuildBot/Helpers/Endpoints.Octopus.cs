using System;
using System.Threading;
using BuildBot.Octopus.Models;
using BuildBot.ServiceModel.Octopus;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace BuildBot.Helpers;

internal static partial class Endpoints
{
    private static WebApplication ConfigureOctopusEndpoints(this WebApplication app)
    {
        Console.WriteLine("Configuring Octopus Endpoint");

        RouteGroupBuilder group = app.MapGroup("/octopus");

        group.MapPost(pattern: "deploy",
                      handler: static async (Deploy model, IMediator mediator, CancellationToken cancellationToken) =>
                               {
                                   await mediator.Publish(new OctopusDeploy(model), cancellationToken: cancellationToken);

                                   return Results.NoContent();
                               });

        return app;
    }
}