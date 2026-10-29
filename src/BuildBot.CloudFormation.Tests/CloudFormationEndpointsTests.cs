using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BuildBot.CloudFormation.Configuration;
using BuildBot.CloudFormation.Models;
using BuildBot.ServiceModel.CloudFormation;
using FunFair.Test.Common;
using FunFair.Test.Infrastructure;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using NSubstitute;
using Xunit;

namespace BuildBot.CloudFormation.Tests;

public sealed class CloudFormationEndpointsTests : TestBase
{
    private const string TOPIC_ARN = "arn:aws:sns:eu-west-1:123456789012:test-topic";
    private const string MESSAGE_ID = "22b80b92-fdea-4c2c-8f9d-bdfb0c7bf324";
    private const string SUBSCRIBE_URL = "https://sns.eu-west-1.amazonaws.com/?Action=ConfirmSubscription";

    private static readonly DateTime Timestamp = new(
        year: 2026,
        month: 1,
        day: 1,
        hour: 0,
        minute: 0,
        second: 0,
        kind: DateTimeKind.Utc
    );

    private readonly IMediator _mediator;
    private readonly ICloudFormationSnsPropertiesParser _parser;

    public CloudFormationEndpointsTests()
    {
        this._mediator = GetSubstitute<IMediator>();
        this._parser = GetSubstitute<ICloudFormationSnsPropertiesParser>();
    }

    [Fact]
    public async Task SubscriptionConfirmationPublishesConfirmationAndReturnsAcceptedAsync()
    {
        SnsMessage message = CreateMessage(type: "SubscriptionConfirmation", subscribeUrl: SUBSCRIBE_URL);

        IResult result = await CloudFormationEndpoints.HandleDeployMessageAsync(
            message: message,
            parser: this._parser,
            mediator: this._mediator,
            cancellationToken: this.CancellationToken()
        );

        Assert.IsType<Accepted>(result);
        CloudFormationSubscriptionConfirmation published =
            this.PublishedNotification<CloudFormationSubscriptionConfirmation>();
        Assert.Equal(expected: TOPIC_ARN, actual: published.TopicArn);
        Assert.Equal(expected: new(SUBSCRIBE_URL), actual: published.SubscribeUrl);
    }

    [Fact]
    public async Task SubscriptionConfirmationWithoutSubscribeUrlReturnsBadRequestAsync()
    {
        SnsMessage message = CreateMessage(type: "SubscriptionConfirmation", subscribeUrl: null);

        IResult result = await CloudFormationEndpoints.HandleDeployMessageAsync(
            message: message,
            parser: this._parser,
            mediator: this._mediator,
            cancellationToken: this.CancellationToken()
        );

        Assert.IsType<BadRequest>(result);
        Assert.Empty(this._mediator.ReceivedCalls());
    }

    [Fact]
    public async Task NotificationPublishesMessageReceivedAndReturnsNoContentAsync()
    {
        SnsMessage message = CreateMessage(type: "Notification", subscribeUrl: null);
        Dictionary<string, string> properties = new(StringComparer.Ordinal) { ["StackName"] = "test-stack" };
        this._parser.SplitMessageToDictionary(message).Returns(properties);

        IResult result = await CloudFormationEndpoints.HandleDeployMessageAsync(
            message: message,
            parser: this._parser,
            mediator: this._mediator,
            cancellationToken: this.CancellationToken()
        );

        Assert.IsType<NoContent>(result);
        CloudFormationMessageReceived published = this.PublishedNotification<CloudFormationMessageReceived>();
        Assert.Equal(expected: TOPIC_ARN, actual: published.TopicArn);
        Assert.Equal(expected: MESSAGE_ID, actual: published.MessageId);
        Assert.Same(expected: properties, actual: published.Properties);
        Assert.Equal(expected: Timestamp, actual: published.Timestamp);
    }

    [Fact]
    public async Task UnknownMessageTypeReturnsNotFoundAsync()
    {
        SnsMessage message = CreateMessage(type: "UnsubscribeConfirmation", subscribeUrl: null);

        IResult result = await CloudFormationEndpoints.HandleDeployMessageAsync(
            message: message,
            parser: this._parser,
            mediator: this._mediator,
            cancellationToken: this.CancellationToken()
        );

        Assert.IsType<NotFound>(result);
        Assert.Empty(this._mediator.ReceivedCalls());
        Assert.Empty(this._parser.ReceivedCalls());
    }

    [Fact]
    public async Task DeployReadsSnsMessageFromBodyAsync()
    {
        const string body =
            "{\"Type\":\"SubscriptionConfirmation\",\"MessageId\":\""
            + MESSAGE_ID
            + "\",\"Token\":\"token\",\"TopicArn\":\""
            + TOPIC_ARN
            + "\",\"Subject\":\"subject\",\"Message\":\"message\",\"Timestamp\":\"2026-01-01T00:00:00Z\","
            + "\"SignatureVersion\":\"1\",\"Signature\":\"signature\",\"SigningCertURL\":null,\"SubscribeURL\":\""
            + SUBSCRIBE_URL
            + "\",\"UnsubscribeURL\":null}";

        IResult result = await this.DeployAsync(body);

        Assert.IsType<Accepted>(result);
        CloudFormationSubscriptionConfirmation published =
            this.PublishedNotification<CloudFormationSubscriptionConfirmation>();
        Assert.Equal(expected: TOPIC_ARN, actual: published.TopicArn);
        Assert.Equal(expected: new(SUBSCRIBE_URL), actual: published.SubscribeUrl);
    }

    [Fact]
    public Task DeployWithNullBodyThrowsAsync()
    {
        return Assert.ThrowsAsync<JsonException>(() => this.DeployAsync("null"));
    }

    [Fact]
    public async Task MapCloudFormationEndpointsMapsDeployRouteAsync()
    {
        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();
        builder
            .Services.AddMockedService<IMediator>()
            .AddCloudFormation(
                new SnsNotificationOptions(
                    TopicArn: TOPIC_ARN,
                    Region: "eu-west-1",
                    AccessKey: "AKIAIOSFODNN7EXAMPLE",
                    SecretKey: "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY"
                )
            );

        await using WebApplication app = builder.Build();

        WebApplication mapped = app.MapCloudFormationEndpoints();
        Assert.Same(expected: app, actual: mapped);

        RouteEndpoint endpoint = Assert.Single(
            ((IEndpointRouteBuilder)app)
                .DataSources.SelectMany(static source => source.Endpoints)
                .OfType<RouteEndpoint>(),
            predicate: static e => StringComparer.Ordinal.Equals(x: e.RoutePattern.RawText, y: "/cloudformation/deploy")
        );

        HttpMethodMetadata? methods = endpoint.Metadata.GetMetadata<HttpMethodMetadata>();
        Assert.NotNull(methods);
        Assert.Equal(expected: [HttpMethods.Post], actual: methods.HttpMethods);

        IAcceptsMetadata? accepts = endpoint.Metadata.GetMetadata<IAcceptsMetadata>();
        Assert.NotNull(accepts);
        Assert.Equal(expected: ["text/plain", "application/json"], actual: accepts.ContentTypes);
    }

    private async Task<IResult> DeployAsync(string body)
    {
        await using MemoryStream stream = new(Encoding.UTF8.GetBytes(body));
        DefaultHttpContext context = new() { Request = { Body = stream } };

        return await CloudFormationEndpoints.DeployAsync(
            request: context.Request,
            parser: this._parser,
            mediator: this._mediator,
            cancellationToken: this.CancellationToken()
        );
    }

    private static SnsMessage CreateMessage(string type, string? subscribeUrl)
    {
        return new(
            type: type,
            messageId: MESSAGE_ID,
            token: null,
            topicArn: TOPIC_ARN,
            subject: "subject",
            message: "message",
            timestamp: Timestamp,
            signatureVersion: "1",
            signature: "signature",
            signingCertUrl: null,
            subscribeUrl: subscribeUrl,
            unsubscribeUrl: null
        );
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
