using System;
using System.Threading;
using BuildBot.GitHub;
using BuildBot.GitHub.Middleware;
using BuildBot.GitHub.Models;
using BuildBot.Helpers.LoggingExtensions;
using BuildBot.ServiceModel.GitHub;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace BuildBot.Helpers;

internal static partial class Endpoints
{
    private static WebApplication ConfigureGitHubEndpoints(this WebApplication app)
    {
        Console.WriteLine("Configuring Github Endpoint");

        if (app.Services.GetRequiredService<GitHubConfiguration>().HasWebhookSecret)
        {
            // Runs after routing and before the endpoint, so unsigned requests are rejected before model binding.
            app.Use(app.Services.GetRequiredService<GitHubWebhookSignatureMiddleware>().InvokeAsync);
        }
        else
        {
            app.Logger.GitHubWebhookSecretNotConfigured();
        }

        RouteGroupBuilder group = app.MapGroup("/github").WithMetadata(new GitHubWebhookEndpointMetadata());

        group.MapPost(
            pattern: "ping",
            handler: static async (PingModel model, IMediator mediator, CancellationToken cancellationToken) =>
            {
                await mediator.Publish(new GithubPing(model), cancellationToken: cancellationToken);

                return Results.NoContent();
            }
        );

        group.MapPost(
            pattern: "push",
            handler: static async (Push model, IMediator mediator, CancellationToken cancellationToken) =>
            {
                await mediator.Publish(new GithubPush(model), cancellationToken: cancellationToken);

                return Results.NoContent();
            }
        );

        group.MapPost(
            pattern: "status",
            handler: static async (Status model, IMediator mediator, CancellationToken cancellationToken) =>
            {
                await mediator.Publish(new GithubStatus(model), cancellationToken: cancellationToken);

                return Results.NoContent();
            }
        );

        return app;
    }
}
