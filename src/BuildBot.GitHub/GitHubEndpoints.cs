using System;
using System.Threading;
using System.Threading.Tasks;
using BuildBot.GitHub.LoggingExtensions;
using BuildBot.GitHub.Middleware;
using BuildBot.GitHub.Models;
using BuildBot.ServiceModel.GitHub;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BuildBot.GitHub;

public static class GitHubEndpoints
{
    public static WebApplication MapGitHubEndpoints(this WebApplication app)
    {
        Console.WriteLine("Configuring Github Endpoint");

        app.UseGitHubWebhookSignatureVerification(
            gitHubConfig: app.Services.GetRequiredService<GitHubConfiguration>(),
            logger: app.Logger
        );

        RouteGroupBuilder group = app.MapGroup("/github").WithMetadata(new GitHubWebhookEndpointMetadata());

        group.MapPost(pattern: "ping", handler: PingAsync);
        group.MapPost(pattern: "push", handler: PushAsync);
        group.MapPost(pattern: "status", handler: StatusAsync);

        return app;
    }

    public static IApplicationBuilder UseGitHubWebhookSignatureVerification(
        this IApplicationBuilder app,
        GitHubConfiguration gitHubConfig,
        ILogger logger
    )
    {
        if (gitHubConfig.HasWebhookSecret)
        {
            // Runs after routing and before the endpoint, so unsigned requests are rejected before model binding.
            return app.Use(app.ApplicationServices.GetRequiredService<GitHubWebhookSignatureMiddleware>().InvokeAsync);
        }

        logger.GitHubWebhookSecretNotConfigured();

        return app;
    }

    public static async Task<IResult> PingAsync(
        PingModel model,
        IMediator mediator,
        CancellationToken cancellationToken
    )
    {
        await mediator.Publish(new GithubPing(model), cancellationToken: cancellationToken);

        return Results.NoContent();
    }

    public static async Task<IResult> PushAsync(Push model, IMediator mediator, CancellationToken cancellationToken)
    {
        await mediator.Publish(new GithubPush(model), cancellationToken: cancellationToken);

        return Results.NoContent();
    }

    public static async Task<IResult> StatusAsync(Status model, IMediator mediator, CancellationToken cancellationToken)
    {
        await mediator.Publish(new GithubStatus(model), cancellationToken: cancellationToken);

        return Results.NoContent();
    }
}
