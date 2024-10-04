using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BuildBot.CloudFormation.Configuration;
using BuildBot.CloudFormation.Models;
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
    private readonly SnsNotificationOptions _options;

    public CloudFormationMessageReceivedNotificationHandler(SnsNotificationOptions options, ILogger<CloudFormationMessageReceivedNotificationHandler> logger)
    {
        this._options = options;
        this._logger = logger;
    }

    public ValueTask Handle(CloudFormationMessageReceived notification, CancellationToken cancellationToken)
    {
        if (!this._options.IsValidArn(notification.TopicArn))
        {
            return ValueTask.CompletedTask;
        }

        this._logger.LogWarning(message: "CLOUDFORMATION: Received message from {TopicArn} with {MessageId} at {Timestamp}", notification.TopicArn, notification.MessageId, notification.Timestamp);

        foreach ((string key, string value) in notification.Properties)
        {
            this._logger.LogWarning(message: "CLOUDFORMATION: Property: {Key} = {Value}", key, value);
        }

        if (!IsMatching(properties: notification.Properties, key: RESOURCE_TYPE, value: "AWS::CloudFormation::Stack"))
        {
            return ValueTask.CompletedTask;
        }

        if (!notification.Properties.TryGetValue(key: STACK_NAME, out string? stackName))
        {
            return ValueTask.CompletedTask;
        }

        if (!notification.Properties.TryGetValue(key: LOGICAL_RESOURCE_ID, out string? project))
        {
            return ValueTask.CompletedTask;
        }

        this._logger.LogWarning(message: "CLOUDFORMATION: LogicalResourceId: {LogicalResourceId}", project);

        if (!notification.Properties.TryGetValue(key: PHYSICAL_RESOURCE_ID, out string? arn))
        {
            return ValueTask.CompletedTask;
        }

        if (!notification.Properties.TryGetValue(key: RESOURCE_STATUS, out string? status))
        {
            return ValueTask.CompletedTask;
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
        }

        return ValueTask.CompletedTask;
    }

    private static bool IsMatching(Dictionary<string, string> properties, string key, string value)
    {
        if (properties.TryGetValue(key: key, out string? propertyValue))
        {
            return StringComparer.Ordinal.Equals(x: propertyValue, y: value);
        }

        return false;
    }
}