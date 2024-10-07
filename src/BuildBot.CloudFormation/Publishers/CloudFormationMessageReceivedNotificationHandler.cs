using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using BuildBot.CloudFormation.Configuration;
using BuildBot.CloudFormation.Models;
using BuildBot.Discord.Models;
using Discord;
using Mediator;
using Microsoft.Extensions.Logging;

namespace BuildBot.CloudFormation.Publishers;

public sealed class CloudFormationMessageReceivedNotificationHandler : INotificationHandler<CloudFormationMessageReceived>
{
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

    public ValueTask Handle(CloudFormationMessageReceived notification, CancellationToken cancellationToken)
    {
        if (!this._options.IsValidArn(notification.TopicArn))
        {
            return ValueTask.CompletedTask;
        }

        Deployment? deployment = this.ExtractDeploymentProperties(notification);

        if (deployment is null)
        {
            return ValueTask.CompletedTask;
        }

        this._logger.LogWarning(message: "CLOUDFORMATION: Building message for {Project} in {StackName}", deployment.Value.Project, deployment.Value.StackName);
        EmbedBuilder embed = BuildStatusMessage(deployment.Value);

        this._logger.LogWarning(message: "CLOUDFORMATION: publishing message for {Project} in {StackName}", deployment.Value.Project, deployment.Value.StackName);

        return this._mediator.Publish(new BotMessage(embed), cancellationToken: cancellationToken);
    }

    private Deployment? ExtractDeploymentProperties(CloudFormationMessageReceived notification)
    {
        this._logger.LogWarning(message: "CLOUDFORMATION: Received message from {TopicArn} with {MessageId} at {Timestamp}", notification.TopicArn, notification.MessageId, notification.Timestamp);

        foreach ((string key, string value) in notification.Properties)
        {
            this._logger.LogWarning(message: "CLOUDFORMATION: Property: {Key} = {Value}", key, value);
        }

        if (!IsMatching(properties: notification.Properties, key: RESOURCE_TYPE, value: "AWS::CloudFormation::Stack"))
        {
            return null;
        }

        if (!notification.Properties.TryGetValue(key: STACK_NAME, out string? stackName))
        {
            return null;
        }

        if (!notification.Properties.TryGetValue(key: LOGICAL_RESOURCE_ID, out string? project))
        {
            return null;
        }

        this._logger.LogWarning(message: "CLOUDFORMATION: LogicalResourceId: {LogicalResourceId}", project);

        if (!notification.Properties.TryGetValue(key: PHYSICAL_RESOURCE_ID, out string? arn))
        {
            return null;
        }

        if (!notification.Properties.TryGetValue(key: RESOURCE_STATUS, out string? status))
        {
            return null;
        }

        this._logger.LogWarning(message: "CLOUDFORMATION: Stack Name: {StackName}", stackName);
        this._logger.LogWarning(message: "CLOUDFORMATION: Project: {Project}", project);
        this._logger.LogWarning(message: "CLOUDFORMATION: Arn: {Arn}", arn);
        this._logger.LogWarning(message: "CLOUDFORMATION: Status: {ResourceStatus}", arn);

        if (Statuses.TryGetValue(key: status, out bool success))
        {
            this._logger.LogWarning(message: success
                                        ? "CLOUDFORMATION: SUCCEEDED!!!"
                                        : "CLOUDFORMATION: FAILED!!!");

            return new Deployment(StackName: stackName, Project: project, Arn: arn, Status: status, Success: success);
        }

        return null;
    }

    private static EmbedBuilder BuildStatusMessage(in Deployment deployment)
    {
        return new EmbedBuilder().WithTitle($"CloudFormation {deployment.Status} for {deployment.Project} in {deployment.StackName}")
                                 .WithColor(deployment.Success
                                                ? Color.Green
                                                : Color.Red)
                                 .WithFields(GetFields(deployment));
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

    [DebuggerDisplay("{StackName} {Project} {Arn} {Status} {Success}")]
    private readonly record struct Deployment(string StackName, string Project, string Arn, string Status, bool Success);
}