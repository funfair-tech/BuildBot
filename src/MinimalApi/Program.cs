using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using BuildBot.ServiceModel.GitHub;
using BuildBot.ServiceModel.Octopus;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MinimalApi.Helpers;
using MinimalApi.Messages;

namespace MinimalApi;

public static class Program
{
    private const int MIN_THREADS = 32;

    [SuppressMessage(category: "Meziantou.Analyzer", checkId: "MA0109: Add an overload with a Span or Memory parameter", Justification = "Won't work here")]
    public static async Task Main(string[] args)
    {
        StartupBanner.Show();

        ServerStartup.SetThreads(MIN_THREADS);

        await using (WebApplication app = ServerStartup.CreateApp(args))
        {
            await app.ConfigureTestEndpoints()
                     .ConfigureGitHubEndpoints()
                     .ConfigureOctopusEndpoints()
                     .RunAsync();
        }
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
                      handler: (Push model) =>
                               {
                                   Console.WriteLine($"Hello {model.Ref}");

                                   return Results.NoContent();
                               });

        group.MapPost(pattern: "status",
                      handler: (Status model) =>
                               {
                                   Console.WriteLine($"Hello {model.TargetUrl}");

                                   return Results.NoContent();
                               });

        return app;
    }

    private static WebApplication ConfigureOctopusEndpoints(this WebApplication app)
    {
        RouteGroupBuilder group = app.MapGroup("/octopus");

        group.MapPost(pattern: "deploy",
                      handler: (Deploy model) =>
                               {
                                   Console.WriteLine($"Hello {model.Timestamp}");

                                   return Results.NoContent();
                               });

        return app;
    }
}