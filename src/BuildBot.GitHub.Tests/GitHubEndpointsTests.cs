using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BuildBot.GitHub.Middleware;
using BuildBot.GitHub.Models;
using BuildBot.ServiceModel.GitHub;
using FunFair.Test.Common;
using FunFair.Test.Infrastructure;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.Core;
using Xunit;

namespace BuildBot.GitHub.Tests;

public sealed class GitHubEndpointsTests : TestBase
{
    private readonly IMediator _mediator;

    public GitHubEndpointsTests()
    {
        this._mediator = GetSubstitute<IMediator>();
    }

    [Fact]
    public async Task PingPublishesGithubPingAndReturnsNoContentAsync()
    {
        PingModel model = new(zen: "Keep it logically awesome.", hookId: "12345");

        IResult result = await GitHubEndpoints.PingAsync(
            model: model,
            mediator: this._mediator,
            cancellationToken: this.CancellationToken()
        );

        Assert.IsType<NoContent>(result);
        GithubPing published = this.PublishedNotification<GithubPing>();
        Assert.Equal(expected: model, actual: published.Model);
    }

    [Fact]
    public async Task PushPublishesGithubPushAndReturnsNoContentAsync()
    {
        Push model = default;

        IResult result = await GitHubEndpoints.PushAsync(
            model: model,
            mediator: this._mediator,
            cancellationToken: this.CancellationToken()
        );

        Assert.IsType<NoContent>(result);
        GithubPush published = this.PublishedNotification<GithubPush>();
        Assert.Equal(expected: model, actual: published.Model);
    }

    [Fact]
    public async Task StatusPublishesGithubStatusAndReturnsNoContentAsync()
    {
        Status model = default;

        IResult result = await GitHubEndpoints.StatusAsync(
            model: model,
            mediator: this._mediator,
            cancellationToken: this.CancellationToken()
        );

        Assert.IsType<NoContent>(result);
        GithubStatus published = this.PublishedNotification<GithubStatus>();
        Assert.Equal(expected: model, actual: published.Model);
    }

    [Fact]
    public void MissingWebhookSecretLogsWarningAndDoesNotAddMiddleware()
    {
        IApplicationBuilder app = GetSubstitute<IApplicationBuilder>();
        ILogger logger = GetSubstitute<ILogger>();
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        app.UseGitHubWebhookSignatureVerification(gitHubConfig: new(string.Empty), logger: logger);

        app.DidNotReceive().Use(Arg.Any<Func<RequestDelegate, RequestDelegate>>());
        ICall logCall = Assert.Single(logger.ReceivedCalls(), predicate: IsLogCall);
        Assert.Equal(expected: LogLevel.Warning, actual: logCall.GetArguments()[0]);
    }

    [Fact]
    public void ConfiguredWebhookSecretAddsMiddlewareWithoutLogging()
    {
        using ServiceProvider serviceProvider = new ServiceCollection()
            .AddLogging()
            .AddMockedService<IMediator>()
            .AddGitHub(new GitHubConfiguration(GitHubWebhookTestVectors.Secret))
            .BuildServiceProvider();

        IApplicationBuilder app = GetSubstitute<IApplicationBuilder>();
        app.ApplicationServices.Returns(serviceProvider);
        ILogger logger = GetSubstitute<ILogger>();
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        app.UseGitHubWebhookSignatureVerification(
            gitHubConfig: serviceProvider.GetRequiredService<GitHubConfiguration>(),
            logger: logger
        );

        app.Received(1).Use(Arg.Any<Func<RequestDelegate, RequestDelegate>>());
        Assert.DoesNotContain(logger.ReceivedCalls(), filter: IsLogCall);
    }

    [Fact]
    public async Task MapGitHubEndpointsMapsWebhookRoutesAsync()
    {
        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();
        builder.Services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.TypeInfoResolverChain.Insert(
                index: 0,
                item: GitHubEndpointsTestSerializationContext.Default
            )
        );
        builder.Services.AddMockedService<IMediator>().AddGitHub(new GitHubConfiguration(string.Empty));

        await using WebApplication app = builder.Build();

        WebApplication mapped = app.MapGitHubEndpoints();
        Assert.Same(expected: app, actual: mapped);

        IReadOnlyList<RouteEndpoint> endpoints =
        [
            .. ((IEndpointRouteBuilder)app)
                .DataSources.SelectMany(static source => source.Endpoints)
                .OfType<RouteEndpoint>(),
        ];

        AssertWebhookPostRoute(endpoints: endpoints, route: "/github/ping");
        AssertWebhookPostRoute(endpoints: endpoints, route: "/github/push");
        AssertWebhookPostRoute(endpoints: endpoints, route: "/github/status");
    }

    private static void AssertWebhookPostRoute(IReadOnlyList<RouteEndpoint> endpoints, string route)
    {
        RouteEndpoint endpoint = Assert.Single(
            endpoints,
            predicate: e => StringComparer.Ordinal.Equals(x: e.RoutePattern.RawText, y: route)
        );

        HttpMethodMetadata? methods = endpoint.Metadata.GetMetadata<HttpMethodMetadata>();
        Assert.NotNull(methods);
        Assert.Equal(expected: [HttpMethods.Post], actual: methods.HttpMethods);
        Assert.NotNull(endpoint.Metadata.GetMetadata<GitHubWebhookEndpointMetadata>());
    }

    private static bool IsLogCall(ICall call)
    {
        return StringComparer.Ordinal.Equals(x: call.GetMethodInfo().Name, y: nameof(ILogger.Log));
    }

    private TNotification PublishedNotification<TNotification>()
        where TNotification : INotification
    {
        return this
            ._mediator.ReceivedCalls()
            .Select(static call => call.GetArguments()[0])
            .OfType<TNotification>()
            .Single();
    }
}
