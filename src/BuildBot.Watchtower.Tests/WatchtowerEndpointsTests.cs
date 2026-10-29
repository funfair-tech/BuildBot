using System;
using System.Linq;
using System.Threading.Tasks;
using BuildBot.ServiceModel.Watchtower;
using BuildBot.Watchtower.Models;
using FunFair.Test.Common;
using FunFair.Test.Infrastructure;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace BuildBot.Watchtower.Tests;

public sealed class WatchtowerEndpointsTests : TestBase
{
    private readonly IMediator _mediator;

    public WatchtowerEndpointsTests()
    {
        this._mediator = GetSubstitute<IMediator>();
    }

    [Fact]
    public async Task DeployPublishesWatchTowerPublishMessageAndReturnsNoContentAsync()
    {
        WatchTowerMessage model = new(Message: "Updated container", Title: "Watchtower");

        IResult result = await WatchtowerEndpoints.DeployAsync(
            model: model,
            mediator: this._mediator,
            cancellationToken: this.CancellationToken()
        );

        Assert.IsType<NoContent>(result);
        WatchTowerPublishMessage published = this.PublishedNotification<WatchTowerPublishMessage>();
        Assert.Equal(expected: model, actual: published.Model);
    }

    [Fact]
    public async Task MapWatchtowerEndpointsMapsDeployRouteAsync()
    {
        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();
        builder.Services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.TypeInfoResolverChain.Insert(
                index: 0,
                item: WatchtowerEndpointsTestSerializationContext.Default
            )
        );
        builder.Services.AddMockedService<IMediator>().AddWatchtower();

        await using WebApplication app = builder.Build();

        WebApplication mapped = app.MapWatchtowerEndpoints();
        Assert.Same(expected: app, actual: mapped);

        RouteEndpoint endpoint = Assert.Single(
            ((IEndpointRouteBuilder)app)
                .DataSources.SelectMany(static source => source.Endpoints)
                .OfType<RouteEndpoint>(),
            predicate: static e => StringComparer.Ordinal.Equals(x: e.RoutePattern.RawText, y: "/watchtower/deploy")
        );

        HttpMethodMetadata? methods = endpoint.Metadata.GetMetadata<HttpMethodMetadata>();
        Assert.NotNull(methods);
        Assert.Equal(expected: [HttpMethods.Post], actual: methods.HttpMethods);
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
