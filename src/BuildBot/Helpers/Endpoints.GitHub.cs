using System;
using System.Threading;
using System.Threading.Tasks;
using BuildBot.GitHub;
using BuildBot.GitHub.Filters;
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

        if (!app.Services.GetRequiredService<IWebhookSignatureVerifier>().IsEnabled)
        {
            app.Logger.GitHubWebhookSecretNotConfigured();
        }

        // The filter is also endpoint metadata so the endpoints it guards are the ones whose body gets buffered.
        app.Use(EnableRequestBufferingAsync);

        GitHubWebhookSignatureFilter signatureFilter = app.Services.GetRequiredService<GitHubWebhookSignatureFilter>();
        RouteGroupBuilder group = app.MapGroup("/github")
            .AddEndpointFilter(signatureFilter)
            .WithMetadata(signatureFilter);

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

    private static Task EnableRequestBufferingAsync(HttpContext context, RequestDelegate next)
    {
        // The signature filter runs after model binding has read the body, so it must be buffered to be re-read.
        if (context.GetEndpoint()?.Metadata.GetMetadata<GitHubWebhookSignatureFilter>() is not null)
        {
            context.Request.EnableBuffering();
        }

        return next(context);
    }
}
