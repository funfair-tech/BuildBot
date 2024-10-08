using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BuildBot.CloudFormation.Configuration;
using BuildBot.CloudFormation.Models;
using BuildBot.CloudFormation.Publishers.LoggingExtensions;
using BuildBot.Discord.Models;
using Discord;
using Mediator;
using Microsoft.Extensions.Logging;

namespace BuildBot.CloudFormation.Publishers;

public sealed class CloudFormationMessageReceivedNotificationHandler : INotificationHandler<CloudFormationMessageReceived>
{
    private readonly IAwsCloudFormation _awsCloudFormation;
    private readonly ICloudFormationDeploymentExtractor _cloudFormationDeploymentExtractor;

    private readonly ILogger<CloudFormationMessageReceivedNotificationHandler> _logger;
    private readonly IMediator _mediator;
    private readonly SnsNotificationOptions _options;

    public CloudFormationMessageReceivedNotificationHandler(SnsNotificationOptions options,
                                                            ICloudFormationDeploymentExtractor cloudFormationDeploymentExtractor,
                                                            IAwsCloudFormation awsCloudFormation,
                                                            IMediator mediator,
                                                            ILogger<CloudFormationMessageReceivedNotificationHandler> logger)
    {
        this._options = options;
        this._cloudFormationDeploymentExtractor = cloudFormationDeploymentExtractor;
        this._awsCloudFormation = awsCloudFormation;
        this._mediator = mediator;
        this._logger = logger;
    }

    public async ValueTask Handle(CloudFormationMessageReceived notification, CancellationToken cancellationToken)
    {
        if (!this._options.IsValidArn(notification.TopicArn))
        {
            this._logger.ReceivedInvalidArn(notification.TopicArn);

            return;
        }

        this._logger.RecievedMessage(notification);

        Deployment? deployment = this._cloudFormationDeploymentExtractor.ExtractDeploymentProperties(notification);

        if (deployment is null)
        {
            return;
        }

        StackDetails? stackDetails = await this._awsCloudFormation.GetStackDetailsAsync(deployment: deployment.Value, cancellationToken: cancellationToken);

        this._logger.BuildingMessage(deployment.Value);
        EmbedBuilder embed = BuildStatusMessage(deployment: deployment.Value, stackDetails: stackDetails);

        this._logger.PublishingMessage(deployment.Value);

        await this._mediator.Publish(new BotMessage(embed), cancellationToken: cancellationToken);
    }

    private static EmbedBuilder BuildStatusMessage(in Deployment deployment, StackDetails? stackDetails)
    {
        EmbedBuilder builder = new EmbedBuilder().WithTitle(BuildTitle(deployment: deployment, stackDetails: stackDetails))
                                                 .WithUrl(BuildStackUrl(deployment)
                                                              .ToString())
                                                 .WithColor(deployment.Success
                                                                ? Color.Green
                                                                : Color.Red)
                                                 .WithFields(GetFields(deployment));

        if (stackDetails is not null)
        {
            builder.WithDescription(stackDetails.Value.Description)
                   .AddField(name: "Version", value: stackDetails.Value.Version);
        }

        return builder;
    }

    private static string BuildTitle(Deployment deployment, StackDetails? stackDetails)
    {
        if (stackDetails is not null && !string.IsNullOrWhiteSpace(stackDetails.Value.Version))
        {
            return deployment.Success
                ? $"{deployment.Project} ({stackDetails.Value.Version}) was deployed "
                : $"{deployment.Project} ({stackDetails.Value.Version}) failed to deploy";
        }

        return deployment.Success
            ? $"{deployment.Project} was deployed "
            : $"{deployment.Project} failed to deploy";
    }

    private static Uri BuildStackUrl(in Deployment deployment)
    {
        // arn%3Aaws%3Acloudformation%3Aeu-west-1%3A117769150821%3Astack%2FBuildBot%2Ff57a70c0-74cb-11ef-8f69-0aa7e4ea5f05
        return new("https://eu-west-1.console.aws.amazon.com/cloudformation/home?region=eu-west-1#/stacks/stackinfo?stackId=" + deployment.StackId);
    }

    private static IReadOnlyList<EmbedFieldBuilder> GetFields(in Deployment deployment)
    {
        return
        [
            AddArnEmbed(deployment),
            AddStatusEmbed(deployment)
        ];
    }

    private static EmbedFieldBuilder AddArnEmbed(in Deployment deployment)
    {
        return new EmbedFieldBuilder().WithName("ARN")
                                      .WithValue(deployment.Arn);
    }

    private static EmbedFieldBuilder AddStatusEmbed(in Deployment deployment)
    {
        return new EmbedFieldBuilder().WithName("Status")
                                      .WithValue(deployment.Status);
    }
}