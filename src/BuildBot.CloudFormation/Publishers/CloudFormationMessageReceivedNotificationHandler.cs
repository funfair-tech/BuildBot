using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using BuildBot.CloudFormation.Configuration;
using BuildBot.CloudFormation.Models;
using BuildBot.CloudFormation.Publishers.LoggingExtensions;
using BuildBot.Discord.Models;
using Mediator;
using Microsoft.Extensions.Logging;

namespace BuildBot.CloudFormation.Publishers;

public sealed class CloudFormationMessageReceivedNotificationHandler
    : INotificationHandler<CloudFormationMessageReceived>
{
    private readonly IAwsCloudFormation _awsCloudFormation;
    private readonly ICloudFormationDeploymentExtractor _cloudFormationDeploymentExtractor;

    private readonly ILogger<CloudFormationMessageReceivedNotificationHandler> _logger;
    private readonly IMediator _mediator;
    private readonly SnsNotificationOptions _options;

    [SuppressMessage(
        category: "Roslynator.Analyzers",
        checkId: "RCS1231: Make parameter ref read-only.",
        Justification = "DI"
    )]
    public CloudFormationMessageReceivedNotificationHandler(
        SnsNotificationOptions options,
        ICloudFormationDeploymentExtractor cloudFormationDeploymentExtractor,
        IAwsCloudFormation awsCloudFormation,
        IMediator mediator,
        ILogger<CloudFormationMessageReceivedNotificationHandler> logger
    )
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

        StackDetails? stackDetails = await this._awsCloudFormation.GetStackDetailsAsync(
            deployment: deployment.Value,
            cancellationToken: cancellationToken
        );

        this._logger.BuildingMessage(deployment.Value);
        DiscordEmbed embed = BuildStatusMessage(deployment: deployment.Value, stackDetails: stackDetails);

        this._logger.PublishingMessage(deployment.Value);

        await this._mediator.Publish(new BotMessage(embed), cancellationToken: cancellationToken);
    }

    private static DiscordEmbed BuildStatusMessage(in Deployment deployment, StackDetails? stackDetails)
    {
        return new(
            Title: BuildTitle(deployment: deployment, stackDetails: stackDetails),
            Url: BuildStackUrl(deployment),
            Description: stackDetails?.Description,
            Color: deployment.Success ? DiscordColor.Green : DiscordColor.Red,
            Fields: GetFields(deployment: deployment, stackDetails: stackDetails)
        );
    }

    private static string BuildTitle(in Deployment deployment, StackDetails? stackDetails)
    {
        if (stackDetails is not null && !string.IsNullOrWhiteSpace(stackDetails.Value.Version))
        {
            return deployment.Success
                ? $"{deployment.Project} ({stackDetails.Value.Version}) was deployed "
                : $"{deployment.Project} ({stackDetails.Value.Version}) failed to deploy";
        }

        return deployment.Success ? $"{deployment.Project} was deployed " : $"{deployment.Project} failed to deploy";
    }

    private static Uri BuildStackUrl(in Deployment deployment)
    {
        // arn%3Aaws%3Acloudformation%3Aeu-west-1%3A117769150821%3Astack%2FBuildBot%2Ff57a70c0-74cb-11ef-8f69-0aa7e4ea5f05
        return new(
            "https://eu-west-1.console.aws.amazon.com/cloudformation/home?region=eu-west-1#/stacks/stackinfo?stackId="
                + deployment.StackId
        );
    }

    private static IReadOnlyList<DiscordEmbedField> GetFields(in Deployment deployment, StackDetails? stackDetails)
    {
        DiscordEmbedField[] fields = [AddArnEmbed(deployment), AddStatusEmbed(deployment)];
        string? version = stackDetails?.Version;

        return string.IsNullOrWhiteSpace(version) ? fields : [.. fields, AddVersionEmbed(version)];
    }

    private static DiscordEmbedField AddArnEmbed(in Deployment deployment)
    {
        return new(Name: "ARN", Value: deployment.Arn);
    }

    private static DiscordEmbedField AddStatusEmbed(in Deployment deployment)
    {
        return new(Name: "Status", Value: deployment.Status);
    }

    private static DiscordEmbedField AddVersionEmbed(string version)
    {
        return new(Name: "Version", Value: version);
    }
}
