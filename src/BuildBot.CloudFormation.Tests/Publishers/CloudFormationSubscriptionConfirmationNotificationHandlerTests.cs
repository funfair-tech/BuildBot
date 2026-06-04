using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BuildBot.CloudFormation.Configuration;
using BuildBot.CloudFormation.Models;
using BuildBot.CloudFormation.Publishers;
using FunFair.Test.Common;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace BuildBot.CloudFormation.Tests.Publishers;

public sealed class CloudFormationSubscriptionConfirmationNotificationHandlerTests : TestBase
{
    private const string ValidArn = "arn:aws:sns:eu-west-1:123:test";

    private (
        SnsNotificationOptions options,
        IHttpClientFactory httpClientFactory,
        ILogger<CloudFormationSubscriptionConfirmationNotificationHandler> logger,
        CloudFormationSubscriptionConfirmationNotificationHandler handler
    ) CreateHandler()
    {
        SnsNotificationOptions options = new(
            TopicArn: ValidArn,
            Region: "eu-west-1",
            AccessKey: "AKIAIOSFODNN7EXAMPLE",
            SecretKey: "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY"
        );
        IHttpClientFactory httpClientFactory = GetSubstitute<IHttpClientFactory>();
        ILogger<CloudFormationSubscriptionConfirmationNotificationHandler> logger =
            this.GetTypedLogger<CloudFormationSubscriptionConfirmationNotificationHandler>();
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        CloudFormationSubscriptionConfirmationNotificationHandler handler = new(
            httpClientFactory: httpClientFactory,
            options: options,
            logger: logger
        );

        return (options, httpClientFactory, logger, handler);
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        category: "SmartAnalyzers.CSharpExtensions.Annotations",
        checkId: "CSE007:DisposeObjectsBeforeLosingScope",
        Justification = "DidNotReceive returns a NSubstitute proxy — no real HttpClient is created"
    )]
    public async Task Handle_WithInvalidArn_DoesNotMakeHttpCall()
    {
        (
            _,
            IHttpClientFactory httpClientFactory,
            _,
            CloudFormationSubscriptionConfirmationNotificationHandler handler
        ) = this.CreateHandler();

        CloudFormationSubscriptionConfirmation notification = new(
            TopicArn: "arn:invalid",
            SubscribeUrl: new Uri("https://test.example.com/subscribe")
        );

        await handler.Handle(notification: notification, cancellationToken: this.CancellationToken());

        httpClientFactory.DidNotReceive().CreateClient(Arg.Any<string>());
    }

    [Fact]
    [SuppressMessage(
        category: "Microsoft.Reliability",
        checkId: "CA2000:DisposeObjectsBeforeLosingScope",
        Justification = "Managed by test lifetime"
    )]
    [SuppressMessage(
        category: "SmartAnalyzers.CSharpExtensions.Annotations",
        checkId: "CSE007:DisposeObjectsBeforeLosingScope",
        Justification = "Managed by test lifetime"
    )]
    public async Task Handle_WithValidArnAndSuccessfulHttpResponse_Succeeds()
    {
        (
            _,
            IHttpClientFactory httpClientFactory,
            _,
            CloudFormationSubscriptionConfirmationNotificationHandler handler
        ) = this.CreateHandler();

        using HttpResponseMessage responseMessage = new(HttpStatusCode.OK);
        using TestHttpMessageHandler messageHandler = new(responseMessage);
        using HttpClient httpClient = new(messageHandler);

        httpClientFactory
            .CreateClient(nameof(CloudFormationSubscriptionConfirmationNotificationHandler))
            .Returns(httpClient);

        CloudFormationSubscriptionConfirmation notification = new(
            TopicArn: ValidArn,
            SubscribeUrl: new Uri("https://test.example.com/subscribe")
        );

        await handler.Handle(notification: notification, cancellationToken: this.CancellationToken());

        httpClientFactory.Received(1).CreateClient(nameof(CloudFormationSubscriptionConfirmationNotificationHandler));
    }

    [Fact]
    [SuppressMessage(
        category: "Microsoft.Reliability",
        checkId: "CA2000:DisposeObjectsBeforeLosingScope",
        Justification = "Managed by test lifetime"
    )]
    [SuppressMessage(
        category: "SmartAnalyzers.CSharpExtensions.Annotations",
        checkId: "CSE007:DisposeObjectsBeforeLosingScope",
        Justification = "Managed by test lifetime"
    )]
    public async Task Handle_WithValidArnAndFailedHttpResponse_LogsError()
    {
        (
            _,
            IHttpClientFactory httpClientFactory,
            ILogger<CloudFormationSubscriptionConfirmationNotificationHandler> logger,
            CloudFormationSubscriptionConfirmationNotificationHandler handler
        ) = this.CreateHandler();

        using HttpResponseMessage responseMessage = new(HttpStatusCode.InternalServerError);
        using TestHttpMessageHandler messageHandler = new(responseMessage);
        using HttpClient httpClient = new(messageHandler);

        httpClientFactory
            .CreateClient(nameof(CloudFormationSubscriptionConfirmationNotificationHandler))
            .Returns(httpClient);

        CloudFormationSubscriptionConfirmation notification = new(
            TopicArn: ValidArn,
            SubscribeUrl: new Uri("https://test.example.com/subscribe")
        );

        await handler.Handle(notification: notification, cancellationToken: this.CancellationToken());

        logger.Received(1).IsEnabled(LogLevel.Error);
    }
}
