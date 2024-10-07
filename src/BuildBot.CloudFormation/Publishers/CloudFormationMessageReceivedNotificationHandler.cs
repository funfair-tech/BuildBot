using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Amazon.Runtime;
using BuildBot.CloudFormation.Configuration;
using BuildBot.CloudFormation.Models;
using BuildBot.Discord.Models;
using Discord;
using Mediator;
using Microsoft.Extensions.Logging;

namespace BuildBot.CloudFormation.Publishers;

public sealed class CloudFormationMessageReceivedNotificationHandler : INotificationHandler<CloudFormationMessageReceived>
{
    private const string STACK_ID = "StackId";
    private const string STACK_NAME = "StackName";
    private const string RESOURCE_TYPE = "ResourceType";
    private const string LOGICAL_RESOURCE_ID = "LogicalResourceId";
    private const string PHYSICAL_RESOURCE_ID = "PhysicalResourceId";
    private const string RESOURCE_STATUS = "ResourceStatus";

    private static readonly Dictionary<string, bool> Statuses = new(StringComparer.Ordinal)
                                                                {
                                                                    ["CREATE_COMPLETE"] = true,
                                                                    ["CREATE_FAILED"] = false,
                                                                    ["DELETE_COMPLETE"] = true,
                                                                    ["DELETE_FAILED"] = false,
                                                                    ["ROLLBACK_COMPLETE"] = false,
                                                                    ["ROLLBACK_IN_PROGRESS"] = false,
                                                                    ["ROLLBACK_FAILED"] = false,
                                                                    ["UPDATE_COMPLETE"] = true,
                                                                    ["UPDATE_ROLLBACK_COMPLETE"] = false,
                                                                    ["UPDATE_ROLLBACK_FAILED"] = false,
                                                                    ["UPDATE_ROLLBACK_IN_PROGRESS"] = false
                                                                };

    private readonly ILogger<CloudFormationMessageReceivedNotificationHandler> _logger;
    private readonly IMediator _mediator;
    private readonly SnsNotificationOptions _options;

    public CloudFormationMessageReceivedNotificationHandler(SnsNotificationOptions options, IMediator mediator, ILogger<CloudFormationMessageReceivedNotificationHandler> logger)
    {
        this._options = options;
        this._mediator = mediator;
        this._logger = logger;
    }

    public async ValueTask Handle(CloudFormationMessageReceived notification, CancellationToken cancellationToken)
    {
        if (!this._options.IsValidArn(notification.TopicArn))
        {
            return;
        }

        Deployment? deployment = this.ExtractDeploymentProperties(notification);

        if (deployment is null)
        {
            return;
        }

        StackDetails? stackDetails = await this.GetStackDetailsAsync(deployment: deployment.Value, cancellationToken: cancellationToken);

        this._logger.LogWarning(message: "CLOUDFORMATION: Building message for {Project} in {StackName}", deployment.Value.Project, deployment.Value.StackName);
        EmbedBuilder embed = BuildStatusMessage(deployment: deployment.Value, stackDetails: stackDetails);

        this._logger.LogWarning(message: "CLOUDFORMATION: publishing message for {Project} in {StackName}", deployment.Value.Project, deployment.Value.StackName);

        await this._mediator.Publish(new BotMessage(embed), cancellationToken: cancellationToken);
    }

    private async ValueTask<StackDetails?> GetStackDetailsAsync(Deployment deployment, CancellationToken cancellationToken)
    {
        RegionEndpoint endpoint = RegionEndpoint.GetBySystemName(this._options.Region);
        AWSCredentials credentials = new BasicAWSCredentials(accessKey: this._options.AccessKey, secretKey: this._options.SecretKey);

        using (AmazonCloudFormationClient cloudFormationClient = new(credentials: credentials, region: endpoint))
        {
            DescribeStacksRequest request = new() { StackName = deployment.StackName };

            DescribeStacksResponse? result = await cloudFormationClient.DescribeStacksAsync(request: request, cancellationToken: cancellationToken);

            if (result is null)
            {
                return null;
            }

            Stack? stack = result.Stacks.FirstOrDefault();

            if (stack is null)
            {
                return null;
            }

            string? projectDescription = stack.Description;

            Tag? versionTag = stack.Tags.Find(t => StringComparer.Ordinal.Equals(x: t.Key, y: "Version"));

            string? projectVersion = versionTag?.Value;

            if (!string.IsNullOrWhiteSpace(projectDescription) || !string.IsNullOrWhiteSpace(projectVersion))
            {
                return new StackDetails(Description: projectDescription, Version: projectVersion);
            }

            return null;
        }
    }

    private Deployment? ExtractDeploymentProperties(CloudFormationMessageReceived notification)
    {
        this._logger.LogWarning(message: "CLOUDFORMATION: Received message from {TopicArn} with {MessageId} at {Timestamp}", notification.TopicArn, notification.MessageId, notification.Timestamp);

        this.DumpAllProperties(notification);

        if (!IsMatching(properties: notification.Properties, key: RESOURCE_TYPE, value: "AWS::CloudFormation::Stack"))
        {
            this._logger.LogWarning(message: "CLOUDFORMATION: Not a cloud formation stack");

            return null;
        }

        if (!notification.Properties.TryGetValue(key: STACK_ID, out string? stackId))
        {
            this._logger.LogWarning(message: "CLOUDFORMATION: No stack id found");

            return null;
        }

        if (!notification.Properties.TryGetValue(key: STACK_NAME, out string? stackName))
        {
            this._logger.LogWarning(message: "CLOUDFORMATION: No stack name found");

            return null;
        }

        if (!notification.Properties.TryGetValue(key: LOGICAL_RESOURCE_ID, out string? project))
        {
            this._logger.LogWarning(message: "CLOUDFORMATION: No logical resource id found");

            return null;
        }

        this._logger.LogWarning(message: "CLOUDFORMATION: LogicalResourceId: {LogicalResourceId}", project);

        if (!notification.Properties.TryGetValue(key: PHYSICAL_RESOURCE_ID, out string? arn))
        {
            this._logger.LogWarning(message: "CLOUDFORMATION: No physical resource id found");

            return null;
        }

        if (!notification.Properties.TryGetValue(key: RESOURCE_STATUS, out string? status))
        {
            this._logger.LogWarning(message: "CLOUDFORMATION: No resource status found");

            return null;
        }

        if (Statuses.TryGetValue(key: status, out bool success))
        {
            this.LogConfiguration(stackName: stackName, project: project, arn: arn, success: success);

            return new Deployment(StackName: stackName, Project: project, Arn: arn, Status: status, Success: success, StackId: stackId);
        }

        this._logger.LogWarning(message: "CLOUDFORMATION: Unknown status");

        return null;
    }

    private void LogConfiguration(string stackName, string project, string arn, bool success)
    {
        this._logger.LogWarning(message: "CLOUDFORMATION: Stack Name: {StackName}", stackName);
        this._logger.LogWarning(message: "CLOUDFORMATION: Project: {Project}", project);
        this._logger.LogWarning(message: "CLOUDFORMATION: Arn: {Arn}", arn);
        this._logger.LogWarning(message: "CLOUDFORMATION: Status: {ResourceStatus}", arn);
        this._logger.LogWarning(message: success
                                    ? "CLOUDFORMATION: SUCCEEDED!!!"
                                    : "CLOUDFORMATION: FAILED!!!");
    }

    private void DumpAllProperties(CloudFormationMessageReceived notification)
    {
        foreach ((string key, string value) in notification.Properties)
        {
            this._logger.LogWarning(message: "CLOUDFORMATION: Property: {Key} = {Value}", key, value);
        }
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

    private static bool IsMatching(Dictionary<string, string> properties, string key, string value)
    {
        if (properties.TryGetValue(key: key, out string? propertyValue))
        {
            return StringComparer.Ordinal.Equals(x: propertyValue, y: value);
        }

        return false;
    }

    [DebuggerDisplay("{Description} {Version}")]
    private readonly record struct StackDetails(string? Description, string? Version);

    [DebuggerDisplay("{StackName} {Project} {Arn} {Status} {Success}")]
    private readonly record struct Deployment(string StackName, string Project, string Arn, string Status, bool Success, string StackId);
}