using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BuildBot.Json.Models;
using BuildBot.ServiceModel.ComponentStatus;
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
using Xunit;

namespace BuildBot.Health.Tests;

public sealed class StatusEndpointsTests : TestBase
{
    private readonly ILogger<StatusEndpointContext> _logger;
    private readonly IMediator _mediator;

    public StatusEndpointsTests()
    {
        this._mediator = GetSubstitute<IMediator>();
        this._logger = GetSubstitute<ILogger<StatusEndpointContext>>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("test")]
    public async Task PingReturnsPongWhenAllServicesAreOkAsync(string? source)
    {
        this.SetStatus(new ServiceStatus(Name: "One", Ok: true), new ServiceStatus(Name: "Two", Ok: true));

        IResult result = await this.PingAsync(source);

        Ok<PongDto> ok = Assert.IsType<Ok<PongDto>>(result);
        Assert.Equal(expected: "Pong!", actual: ok.Value.Value);
        await this._mediator.Received(1).Send(new CheckStatus(source), this.CancellationToken());
    }

    [Fact]
    public async Task PingReturnsConflictWhenAnyServiceIsNotOkAsync()
    {
        ServiceStatus[] statuses = [new(Name: "One", Ok: true), new(Name: "Two", Ok: false)];
        this.SetStatus(statuses);

        IResult result = await this.PingAsync("test");

        Conflict<IReadOnlyList<ServiceStatus>> conflict = Assert.IsType<Conflict<IReadOnlyList<ServiceStatus>>>(result);
        Assert.Equal(expected: statuses, actual: conflict.Value);
    }

    [Fact]
    public async Task PingReturnsPongWhenNoServicesAreRegisteredAsync()
    {
        this.SetStatus();

        IResult result = await this.PingAsync(null);

        Assert.IsType<Ok<PongDto>>(result);
    }

    [Fact]
    public async Task MapStatusEndpointsMapsPingRouteAsync()
    {
        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();
        builder.Services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.TypeInfoResolverChain.Insert(
                index: 0,
                item: StatusEndpointsTestSerializationContext.Default
            )
        );
        builder.Services.AddMockedService<IMediator>();

        await using WebApplication app = builder.Build();

        WebApplication mapped = app.MapStatusEndpoints();
        Assert.Same(expected: app, actual: mapped);

        RouteEndpoint endpoint = Assert.Single(
            ((IEndpointRouteBuilder)app)
                .DataSources.SelectMany(static source => source.Endpoints)
                .OfType<RouteEndpoint>(),
            predicate: static e => StringComparer.Ordinal.Equals(x: e.RoutePattern.RawText, y: "/ping")
        );

        HttpMethodMetadata? methods = endpoint.Metadata.GetMetadata<HttpMethodMetadata>();
        Assert.NotNull(methods);
        Assert.Equal(expected: [HttpMethods.Get], actual: methods.HttpMethods);
    }

    private Task<IResult> PingAsync(string? source)
    {
        return StatusEndpoints.PingAsync(
            source: source,
            mediator: this._mediator,
            logger: this._logger,
            cancellationToken: this.CancellationToken()
        );
    }

    private void SetStatus(params ServiceStatus[] statuses)
    {
        IReadOnlyList<ServiceStatus> result = statuses;

        this._mediator.Send(Arg.Any<CheckStatus>(), Arg.Any<CancellationToken>()).Returns(result);
    }
}
