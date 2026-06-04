using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BuildBot.CloudFormation;
using BuildBot.CloudFormation.Configuration;
using BuildBot.CloudFormation.Models;
using BuildBot.CloudFormation.Publishers;
using BuildBot.Discord.Models;
using FunFair.Test.Common;
using FunFair.Test.Common.Mocks;
using Mediator;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace BuildBot.CloudFormation.Tests.Publishers;

public sealed class CloudFormationMessageReceivedNotificationHandlerTests : TestBase
{
    private const string ValidArn = "arn:aws:sns:eu-west-1:123:test";

    private (
        SnsNotificationOptions options,
        ICloudFormationDeploymentExtractor extractor,
        IAwsCloudFormation aws,
        IMediator mediator,
        CloudFormationMessageReceivedNotificationHandler handler
    ) CreateHandler()
    {
        SnsNotificationOptions options = new(
            TopicArn: ValidArn,
            Region: "eu-west-1",
            AccessKey: "AKIAIOSFODNN7EXAMPLE",
            SecretKey: "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY"
        );
        ICloudFormationDeploymentExtractor extractor = GetSubstitute<ICloudFormationDeploymentExtractor>();
        IAwsCloudFormation aws = GetSubstitute<IAwsCloudFormation>();
        IMediator mediator = GetSubstitute<IMediator>();
        ILogger<CloudFormationMessageReceivedNotificationHandler> logger =
            this.GetTypedLogger<CloudFormationMessageReceivedNotificationHandler>();
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        CloudFormationMessageReceivedNotificationHandler handler = new(
            options: options,
            cloudFormationDeploymentExtractor: extractor,
            awsCloudFormation: aws,
            mediator: mediator,
            logger: logger
        );

        return (options, extractor, aws, mediator, handler);
    }

    private static CloudFormationMessageReceived MakeNotification(string topicArn)
    {
        return new CloudFormationMessageReceived(
            TopicArn: topicArn,
            MessageId: "msg-1",
            Properties: new Dictionary<string, string>(StringComparer.Ordinal),
            Timestamp: MockDateTimeSources.Past.GetUtcNow().UtcDateTime
        );
    }

    private static Deployment MakeDeployment()
    {
        return new Deployment(
            StackName: "MyStack",
            Project: "MyProject",
            Arn: "arn:aws:cf:eu-west-1:123:stack/MyStack/x",
            Status: "UPDATE_COMPLETE",
            Success: true,
            StackId: "arn:aws:cf:eu-west-1:123:stack/MyStack/x"
        );
    }

    [Fact]
    public async Task Handle_WithInvalidArn_DoesNotPublish()
    {
        (_, _, _, IMediator mediator, CloudFormationMessageReceivedNotificationHandler handler) = this.CreateHandler();

        CloudFormationMessageReceived notification = MakeNotification("arn:invalid");

        await handler.Handle(notification: notification, cancellationToken: this.CancellationToken());

        await mediator.DidNotReceive().Publish(Arg.Any<BotMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithValidArnButNoDeployment_DoesNotPublish()
    {
        (
            _,
            ICloudFormationDeploymentExtractor extractor,
            _,
            IMediator mediator,
            CloudFormationMessageReceivedNotificationHandler handler
        ) = this.CreateHandler();

        extractor.ExtractDeploymentProperties(Arg.Any<CloudFormationMessageReceived>()).Returns((Deployment?)null);

        CloudFormationMessageReceived notification = MakeNotification(ValidArn);

        await handler.Handle(notification: notification, cancellationToken: this.CancellationToken());

        await mediator.DidNotReceive().Publish(Arg.Any<BotMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithValidDeploymentAndNoStackDetails_PublishesSuccessMessage()
    {
        (
            _,
            ICloudFormationDeploymentExtractor extractor,
            IAwsCloudFormation aws,
            IMediator mediator,
            CloudFormationMessageReceivedNotificationHandler handler
        ) = this.CreateHandler();

        Deployment deployment = MakeDeployment();
        extractor.ExtractDeploymentProperties(Arg.Any<CloudFormationMessageReceived>()).Returns(deployment);
        aws.GetStackDetailsAsync(Arg.Any<Deployment>(), Arg.Any<CancellationToken>()).Returns((StackDetails?)null);

        CloudFormationMessageReceived notification = MakeNotification(ValidArn);

        await handler.Handle(notification: notification, cancellationToken: this.CancellationToken());

        await mediator.Received(1).Publish(Arg.Any<BotMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithValidDeploymentAndStackDetails_PublishesMessageWithVersion()
    {
        (
            _,
            ICloudFormationDeploymentExtractor extractor,
            IAwsCloudFormation aws,
            IMediator mediator,
            CloudFormationMessageReceivedNotificationHandler handler
        ) = this.CreateHandler();

        Deployment deployment = MakeDeployment();
        extractor.ExtractDeploymentProperties(Arg.Any<CloudFormationMessageReceived>()).Returns(deployment);
        aws.GetStackDetailsAsync(Arg.Any<Deployment>(), Arg.Any<CancellationToken>())
            .Returns(new StackDetails(Description: "A deployment", Version: "1.0.0"));

        CloudFormationMessageReceived notification = MakeNotification(ValidArn);

        await handler.Handle(notification: notification, cancellationToken: this.CancellationToken());

        await mediator.Received(1).Publish(Arg.Any<BotMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithFailedDeployment_PublishesFailureMessage()
    {
        (
            _,
            ICloudFormationDeploymentExtractor extractor,
            IAwsCloudFormation aws,
            IMediator mediator,
            CloudFormationMessageReceivedNotificationHandler handler
        ) = this.CreateHandler();

        Deployment deployment = new(
            StackName: "MyStack",
            Project: "MyProject",
            Arn: "arn:aws:cf:eu-west-1:123:stack/MyStack/x",
            Status: "UPDATE_FAILED",
            Success: false,
            StackId: "arn:aws:cf:eu-west-1:123:stack/MyStack/x"
        );
        extractor.ExtractDeploymentProperties(Arg.Any<CloudFormationMessageReceived>()).Returns(deployment);
        aws.GetStackDetailsAsync(Arg.Any<Deployment>(), Arg.Any<CancellationToken>()).Returns((StackDetails?)null);

        CloudFormationMessageReceived notification = MakeNotification(ValidArn);

        await handler.Handle(notification: notification, cancellationToken: this.CancellationToken());

        await mediator.Received(1).Publish(Arg.Any<BotMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithFailedDeploymentAndStackDetails_PublishesFailureMessageWithVersion()
    {
        (
            _,
            ICloudFormationDeploymentExtractor extractor,
            IAwsCloudFormation aws,
            IMediator mediator,
            CloudFormationMessageReceivedNotificationHandler handler
        ) = this.CreateHandler();

        Deployment deployment = new(
            StackName: "MyStack",
            Project: "MyProject",
            Arn: "arn:aws:cf:eu-west-1:123:stack/MyStack/x",
            Status: "UPDATE_FAILED",
            Success: false,
            StackId: "arn:aws:cf:eu-west-1:123:stack/MyStack/x"
        );
        extractor.ExtractDeploymentProperties(Arg.Any<CloudFormationMessageReceived>()).Returns(deployment);
        aws.GetStackDetailsAsync(Arg.Any<Deployment>(), Arg.Any<CancellationToken>())
            .Returns(new StackDetails(Description: "A deployment", Version: "2.0.0"));

        CloudFormationMessageReceived notification = MakeNotification(ValidArn);

        await handler.Handle(notification: notification, cancellationToken: this.CancellationToken());

        await mediator.Received(1).Publish(Arg.Any<BotMessage>(), Arg.Any<CancellationToken>());
    }
}
