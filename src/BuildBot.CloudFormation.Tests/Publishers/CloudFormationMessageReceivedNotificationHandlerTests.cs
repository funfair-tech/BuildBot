using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BuildBot.CloudFormation;
using BuildBot.CloudFormation.Configuration;
using BuildBot.CloudFormation.Models;
using BuildBot.CloudFormation.Publishers;
using BuildBot.Discord.Models;
using FunFair.Test.Common;
using FunFair.Test.Infrastructure.Mocks;
using Mediator;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace BuildBot.CloudFormation.Tests.Publishers;

public sealed class CloudFormationMessageReceivedNotificationHandlerTests : TestBase
{
    private const string VALID_ARN = "arn:aws:sns:eu-west-1:123:test";

    private (
        SnsNotificationOptions options,
        ICloudFormationDeploymentExtractor extractor,
        IAwsCloudFormation aws,
        IMediator mediator,
        CloudFormationMessageReceivedNotificationHandler handler
    ) CreateHandler()
    {
        SnsNotificationOptions options = new(
            TopicArn: VALID_ARN,
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

    [SuppressMessage(
        category: "Microsoft.IDE",
        checkId: "IDE0028",
        Justification = "Roslyn's IDE0028 codefix for Dictionary construction with an explicit IEqualityComparer<string> drops the comparer, which then violates MA0002; see https://github.com/dotnet/roslyn/issues/75894"
    )]
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

        CloudFormationMessageReceived notification = MakeNotification(VALID_ARN);

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

        CloudFormationMessageReceived notification = MakeNotification(VALID_ARN);

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

        CloudFormationMessageReceived notification = MakeNotification(VALID_ARN);

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

        CloudFormationMessageReceived notification = MakeNotification(VALID_ARN);

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

        CloudFormationMessageReceived notification = MakeNotification(VALID_ARN);

        await handler.Handle(notification: notification, cancellationToken: this.CancellationToken());

        await mediator.Received(1).Publish(Arg.Any<BotMessage>(), Arg.Any<CancellationToken>());
    }

    private static DiscordEmbed PublishedEmbed(IMediator mediator)
    {
        return mediator
            .ReceivedCalls()
            .Select(static call => call.GetArguments()[0])
            .OfType<BotMessage>()
            .Single()
            .Message;
    }

    private async Task<DiscordEmbed> PublishAsync(Deployment deployment, StackDetails? stackDetails)
    {
        (
            _,
            ICloudFormationDeploymentExtractor extractor,
            IAwsCloudFormation aws,
            IMediator mediator,
            CloudFormationMessageReceivedNotificationHandler handler
        ) = this.CreateHandler();

        extractor.ExtractDeploymentProperties(Arg.Any<CloudFormationMessageReceived>()).Returns(deployment);
        aws.GetStackDetailsAsync(Arg.Any<Deployment>(), Arg.Any<CancellationToken>()).Returns(stackDetails);

        await handler.Handle(notification: MakeNotification(VALID_ARN), cancellationToken: this.CancellationToken());

        return PublishedEmbed(mediator);
    }

    [Fact]
    public async Task Handle_WithoutStackDetails_PublishesExpectedEmbed()
    {
        DiscordEmbed embed = await this.PublishAsync(deployment: MakeDeployment(), stackDetails: null);

        Assert.Equal(expected: "MyProject was deployed ", actual: embed.Title);
        Assert.Equal(
            expected: new Uri(
                "https://eu-west-1.console.aws.amazon.com/cloudformation/home?region=eu-west-1#/stacks/stackinfo?stackId=arn:aws:cf:eu-west-1:123:stack/MyStack/x"
            ),
            actual: embed.Url
        );
        Assert.Null(embed.Description);
        Assert.Equal(expected: 0x2ECC71u, actual: embed.Color?.Value);
        Assert.Equal(
            expected:
            [
                new DiscordEmbedField(Name: "ARN", Value: "arn:aws:cf:eu-west-1:123:stack/MyStack/x"),
                new DiscordEmbedField(Name: "Status", Value: "UPDATE_COMPLETE"),
            ],
            actual: embed.Fields
        );
    }

    [Fact]
    public async Task Handle_WithFailedDeploymentAndStackDetails_PublishesExpectedEmbed()
    {
        Deployment deployment = MakeDeployment() with { Status = "UPDATE_FAILED", Success = false };

        DiscordEmbed embed = await this.PublishAsync(
            deployment: deployment,
            stackDetails: new StackDetails(Description: "A deployment", Version: "2.0.0")
        );

        Assert.Equal(expected: "MyProject (2.0.0) failed to deploy", actual: embed.Title);
        Assert.Equal(expected: "A deployment", actual: embed.Description);
        Assert.Equal(expected: 0xE74C3Cu, actual: embed.Color?.Value);
        Assert.Equal(
            expected:
            [
                new DiscordEmbedField(Name: "ARN", Value: "arn:aws:cf:eu-west-1:123:stack/MyStack/x"),
                new DiscordEmbedField(Name: "Status", Value: "UPDATE_FAILED"),
                new DiscordEmbedField(Name: "Version", Value: "2.0.0"),
            ],
            actual: embed.Fields
        );
    }

    [Fact]
    public async Task Handle_WithStackDetailsButNoVersion_OmitsVersionField()
    {
        DiscordEmbed embed = await this.PublishAsync(
            deployment: MakeDeployment(),
            stackDetails: new StackDetails(Description: "A deployment", Version: null)
        );

        Assert.Equal(expected: "MyProject was deployed ", actual: embed.Title);
        Assert.Equal(expected: "A deployment", actual: embed.Description);
        Assert.Equal(
            expected: ["ARN", "Status"],
            actual: embed.Fields.Select(static f => f.Name),
            comparer: StringComparer.Ordinal
        );
    }
}
