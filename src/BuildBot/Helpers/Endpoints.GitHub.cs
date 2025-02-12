using System;
using System.Threading;
using BuildBot.GitHub.Models;
using BuildBot.ServiceModel.GitHub;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace BuildBot.Helpers;

internal static partial class Endpoints
{
    private static WebApplication ConfigureGitHubEndpoints(this WebApplication app)
    {
        Console.WriteLine("Configuring Github Endpoint");

        RouteGroupBuilder group = app.MapGroup("/github");
        group.MapPost(
            pattern: "ping",
            handler: static async (
                PingModel model,
                IMediator mediator,
                CancellationToken cancellationToken
            ) =>
            {
                await mediator.Publish(new GithubPing(model), cancellationToken: cancellationToken);

                return Results.NoContent();
            }
        );

        group.MapPost(
            pattern: "push",
            handler: static async (
                Push model,
                IMediator mediator,
                CancellationToken cancellationToken
            ) =>
            {
                await mediator.Publish(new GithubPush(model), cancellationToken: cancellationToken);

                return Results.NoContent();
            }
        );

        group.MapPost(
            pattern: "status",
            handler: static async (
                Status model,
                IMediator mediator,
                CancellationToken cancellationToken
            ) =>
            {
                await mediator.Publish(
                    new GithubStatus(model),
                    cancellationToken: cancellationToken
                );

                return Results.NoContent();
            }
        );

        return app;
    }
}
