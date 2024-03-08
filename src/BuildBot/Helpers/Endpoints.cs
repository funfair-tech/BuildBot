using System.Threading;
using BuildBot.GitHub.Models;
using BuildBot.Octopus.Models;
using BuildBot.ServiceModel.GitHub;
using BuildBot.ServiceModel.Octopus;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace BuildBot.Helpers;

internal static class Endpoints
{
    public static WebApplication ConfigureEndpoints(this WebApplication app)
    {
        return app.ConfigureTestEndpoints()
                  .ConfigureGitHubEndpoints()
                  .ConfigureOctopusEndpoints();
    }

    private static WebApplication ConfigureTestEndpoints(this WebApplication app)
    {
        app.MapGet(pattern: "/ping", handler: () => Results.Ok(PingPong.Model));

        return app;
    }

    private static WebApplication ConfigureGitHubEndpoints(this WebApplication app)
    {
        RouteGroupBuilder group = app.MapGroup("/github");
        group.MapPost(pattern: "ping",
                      handler: async (PingModel model, IMediator mediator, CancellationToken cancellationToken) =>
                               {
                                   await mediator.Publish(new GithubPing(model), cancellationToken: cancellationToken);

                                   return Results.NoContent();
                               });

        group.MapPost(pattern: "push",
                      handler: async (Push model, IMediator mediator, CancellationToken cancellationToken) =>
                               {
                                   await mediator.Publish(new GithubPush(model), cancellationToken: cancellationToken);

                                   return Results.NoContent();
                               });

        group.MapPost(pattern: "status",
                      handler: async (Status model, IMediator mediator, CancellationToken cancellationToken) =>
                               {
                                   await mediator.Publish(new GithubStatus(model), cancellationToken: cancellationToken);

                                   return Results.NoContent();
                               });

        return app;
    }

    private static WebApplication ConfigureOctopusEndpoints(this WebApplication app)
    {
        RouteGroupBuilder group = app.MapGroup("/octopus");

        group.MapPost(pattern: "deploy",
                      handler: async (Deploy model, IMediator mediator, CancellationToken cancellationToken) =>
                               {
                                   await mediator.Publish(new OctopusDeploy(model), cancellationToken: cancellationToken);

                                   return Results.NoContent();
                               });

        return app;
    }
}